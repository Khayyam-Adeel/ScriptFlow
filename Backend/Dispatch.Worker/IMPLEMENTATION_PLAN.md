# Dispatch.Worker Implementation Plan

## Context

`Backend/Dispatch.Worker/Spec.md` (the file you referenced) is empty — the real content
is in `Backend/Dispatch.Worker/DeliveryServiceSpec.md` (it looks like this got moved out
of `SPEC/DeliveryServiceSpec.md` into the worker's own folder; `git status` shows the old
path deleted and this one untracked). This plan is built from that file.

The spec: Dispatch.Worker consumes `PrescriptionSigned` events, calls a pharmacy gateway,
retries transient failures with backoff, dead-letters poison messages, is idempotent, and
publishes an acknowledgement (`PrescriptionAcknowledged` / `PrescriptionRejected`) back
out. Today `Dispatch.Worker` is the untouched Worker Service template (`Worker.cs` does
nothing) and `PharmacyGateway.mock` is the untouched Web API template (no endpoints) — so
there's nothing to dispatch *to* yet.

`ScriptFlow.API` (already fully implemented) is the reference for structure and style:
clean layering (`Domain`/`Application`/`Infrastructure`), MediatR-less plain handlers here
(no HTTP requests to route, so no need for MediatR in a worker), `Shared.Infrastructure`
for cross-cutting plumbing (correlation IDs, Serilog, RabbitMQ), constructor injection,
one class per file, XML-doc-style comments only where *why* isn't obvious from naming.

Decisions confirmed with you before writing this plan:
- **PharmacyGateway.mock is in scope** — Dispatch.Worker needs something real to call.
- **RabbitMQ consumer plumbing goes into `Shared.Infrastructure`**, next to the existing
  `IEventPublisher`/`RabbitMqEventPublisher`, since `Notification.Service` will need the
  same consume-with-DLQ pattern later.
- **Retry/backoff uses Polly**, attached at the HTTP-client layer (`AddPolicyHandler`),
  not a hand-rolled loop.
- **Idempotency**: implementation this pass is in-memory (matching `ScriptFlow.API`'s
  current in-memory-only persistence), but I will add the durable table design to
  `SPEC/DatabaseSpec.md` now, per your instruction, so the real store is a drop-in once
  you wire up SQL Server.

## How the pieces fit together

```
ScriptFlow.API                         Dispatch.Worker                    PharmacyGateway.mock
───────────────                         ───────────────                    ─────────────────────
SignPrescriptionCommandHandler
  publishes PrescriptionSignedEvent
        │
        ▼
  [scriptflow.events] topic exchange
        │  routing key: "PrescriptionSignedEvent"
        ▼
  [dispatch.prescription-signed] queue ──▶ Worker.cs (BackgroundService)
                                              │
                                              ▼
                                   PrescriptionSignedEventHandler
                                     1. already processed? (idempotency) → ack, skip
                                     2. IPharmacyGatewayClient.DispatchAsync(...)  ───▶ POST /api/pharmacy/dispatch
                                        (Polly: 3 retries, 2s/4s/8s backoff,             │ random delay
                                         on HttpRequestException/TaskCanceled)           │ then Ack (60%) /
                                              │                                          │ Reject (20%) /
                                              │◀── Acknowledged / Rejected response ─────┘ Drop→abort conn (20%)
                                              ▼
                                     publish PrescriptionAcknowledgedEvent
                                        or PrescriptionRejectedEvent
                                              │
                                              ▼  (all retries exhausted → exception bubbles up)
                                   consumer Nacks(requeue:false) ──▶ [dispatch.prescription-signed.dlq]
```

Two outcomes are deliberately different code paths:
- **Rejected** is a legitimate business answer from the pharmacy (e.g. out of stock) —
  not an error, not retried, always acked, always published as `PrescriptionRejectedEvent`.
- **Drop** (simulated as an aborted HTTP connection) is a transient *failure* — this is
  what Polly retries against. Only once retries are exhausted does the message become
  poison and go to the DLQ.

## New shared contracts (`Backend/Shared.contract`)

Both `Dispatch.Worker` and `PharmacyGateway.mock` need to agree on the HTTP request/response
shape, and `PharmacyGateway.mock` only references `Shared.contract` (not `Shared.Events`/
`Shared.Infrastructure`), so this is where it belongs:
- `Contracts/PharmacyDispatchRequest.cs` — `PrescriptionId`, `Scid`, `CorrelationId`.
- `Contracts/PharmacyDispatchResponse.cs` — `Outcome` (enum below), `PharmacyReference`
  (nullable, set on Acknowledged), `RejectionReason` (nullable, set on Rejected).
- `Enums/PharmacyDispatchOutcome.cs` — `Acknowledged`, `Rejected`. (`Drop` has no enum
  value — it's a severed connection, not a parsed response.)

## New integration events (`Backend/Shared.Events`)

Mirroring the existing `PrescriptionSignedEvent`:
- `PrescriptionAcknowledgedEvent : IntegrationEvent` — `PrescriptionId`, `Scid`,
  `PharmacyReference`, `AcknowledgedAtUtc`, `Status` (`PrescriptionStatus.Acknowledged`).
- `PrescriptionRejectedEvent : IntegrationEvent` — `PrescriptionId`, `Scid`,
  `RejectionReason`, `RejectedAtUtc`, `Status` (`PrescriptionStatus.Rejected`).

## New consumer plumbing (`Backend/Shared.Infrastructure/Messaging`)

Alongside the existing `IEventPublisher`/`RabbitMqEventPublisher`/`RabbitMqOptions`:
- `RabbitMqConsumerSettings.cs` — per-consumer topology a service supplies at startup:
  `QueueName`, `RoutingKey`, `DeadLetterExchangeName` (default `scriptflow.events.dlx`),
  `DeadLetterQueueName`, `PrefetchCount` (default 10).
- `IEventConsumer<TEvent>.cs` — `Task ConsumeAsync(Func<TEvent, CancellationToken, Task> onMessage, CancellationToken stoppingToken)`.
  One long-running call a `BackgroundService` awaits; the caller supplies the handler
  delegate, the consumer supplies the RabbitMQ plumbing.
- `RabbitMqEventConsumer.cs` — implements it: declares the shared topic exchange
  (defensive, matches the publisher), declares the DLX + DLQ and binds them, declares the
  main queue with `x-dead-letter-exchange` pointing at the DLX, binds the main queue to
  the topic exchange on `RoutingKey`, sets QoS/prefetch, subscribes an
  `EventingBasicConsumer`. On each message: deserializes JSON, calls
  `CorrelationIdAccessor.Set(...)` + `LogContext.PushProperty("CorrelationId", ...)` from
  the event's own `CorrelationId` (same pattern `CorrelationIdMiddleware` uses for HTTP),
  invokes `onMessage`. Success → `BasicAck`. Any exception (deserialize failure or handler
  failure after Polly gives up) → `BasicNack(requeue: false)`, which RabbitMQ routes to
  the DLQ automatically because of the queue's dead-letter argument — no manual retry
  topology needed.
- `RabbitMqConsumerServiceCollectionExtensions.cs` — `AddRabbitMqConsumer<TEvent>(settings)`
  registers `IEventConsumer<TEvent>` reading the existing `IOptions<RabbitMqOptions>` for
  connection details plus the settings above.

## Dispatch.Worker layout (mirrors ScriptFlow.API's Domain/Application/Infrastructure split)

```
Backend/Dispatch.Worker/
  Application/
    Interfaces/
      IPharmacyGatewayClient.cs        Task<PharmacyDispatchResponse> DispatchAsync(request, ct)
      IProcessedMessageStore.cs        Task<bool> IsProcessedAsync(eventId, ct); Task MarkProcessedAsync(eventId, ct)
    Handlers/
      PrescriptionSignedEventHandler.cs   orchestrates idempotency check → pharmacy call → publish outcome event
  Infrastructure/
    Pharmacy/
      PharmacyGatewayOptions.cs        BaseUrl, TimeoutSeconds (bound from appsettings)
      PharmacyGatewayHttpClient.cs     IPharmacyGatewayClient impl, POSTs to /api/pharmacy/dispatch
    Idempotency/
      InMemoryProcessedMessageStore.cs ConcurrentDictionary<Guid,byte>, singleton — documented limitation (resets on restart)
    DispatchWorkerServiceCollectionExtensions.cs   DI wiring, see below
  Worker.cs                            thin BackgroundService: await consumer.ConsumeAsync(handler.HandleAsync, stoppingToken)
  Program.cs                           composition root
```

`DispatchWorkerServiceCollectionExtensions.AddDispatchWorker(configuration)` registers:
- `IProcessedMessageStore` → `InMemoryProcessedMessageStore` (singleton).
- Typed HTTP client for `IPharmacyGatewayClient` → `PharmacyGatewayHttpClient`, with a
  Polly retry policy attached via `AddPolicyHandler`: retry on `HttpRequestException` or
  request timeout, 3 attempts, exponential backoff (2s, 4s, 8s) — this is what absorbs the
  mock gateway's simulated "drop".
- `PrescriptionSignedEventHandler`.
- `services.AddRabbitMqConsumer<PrescriptionSignedEvent>(new RabbitMqConsumerSettings { QueueName = "dispatch.prescription-signed", RoutingKey = nameof(PrescriptionSignedEvent), DeadLetterQueueName = "dispatch.prescription-signed.dlq" })`.

`Program.cs`: `Host.CreateApplicationBuilder(args)` → Serilog bootstrap via the existing
`SerilogExtensions.CreateBaseConfiguration(configuration, "Dispatch.Worker")` →
`builder.Services.AddSharedInfrastructure(configuration)` (gets `IEventPublisher` +
`ICorrelationIdAccessor` for free, same as `ScriptFlow.API`) → `AddDispatchWorker(configuration)`
→ `AddHostedService<Worker>()`.

`appsettings.json` gains `RabbitMq` (HostName/Port/UserName/Password/ExchangeName — same
shape `ScriptFlow.API` already uses) and `PharmacyGateway:BaseUrl` sections.

## PharmacyGateway.mock

`Controllers/PharmacyController.cs` (matches `ScriptFlow.API`'s controller-based style):
`POST /api/pharmacy/dispatch` accepts `PharmacyDispatchRequest`. Logic, heavily commented
since this *is* the "randomly delay/ack/nack/drop" requirement from `MainSpec.md`:
1. `await Task.Delay(Random.Shared.Next(100, 2000))` — simulate a slow gateway.
2. Roll a weighted outcome: ~60% Acknowledged, ~20% Rejected, ~20% Drop.
3. Acknowledged → 200 with `PharmacyDispatchResponse { Outcome = Acknowledged, PharmacyReference = Guid.NewGuid() }`.
4. Rejected → 200 with `PharmacyDispatchResponse { Outcome = Rejected, RejectionReason = "OutOfStock" }` (a fixed reason is enough to demonstrate the path).
5. Drop → `HttpContext.Abort()` — genuinely severs the connection so the caller sees a
   real transport failure (not a fake status code), which is what should trigger Polly's
   retry client-side.

No new packages needed; add `[ApiController]`/`AddControllers()` usage (the template
already calls `AddControllers()`/`MapControllers()`, just no controller exists yet).

## SPEC/DatabaseSpec.md addition

One new table, following the existing conventions (audit columns, `UNIQUEIDENTIFIER` PKs):
- **`ProcessedMessages`** — durable idempotency ledger for event consumers: `EventId`
  (PK, matches `IntegrationEvent.EventId`), `EventType` (`NVARCHAR(200)`), `PrescriptionId`,
  `ProcessedAtUtc`. A unique constraint on `EventId` is what makes re-delivery a no-op.
  Documented as the future replacement for `InMemoryProcessedMessageStore` once SQL Server
  is wired up — same "deferred" pattern already used for the rest of the schema.

## NuGet packages to add

- `Dispatch.Worker`: `Microsoft.Extensions.Http.Polly` (brings `Polly` transitively, adds
  `AddPolicyHandler` on `IHttpClientBuilder`). `RabbitMQ.Client` and `Serilog.AspNetCore`
  are already available transitively via the `Shared.Infrastructure` project reference.
- `PharmacyGateway.mock`: none — already references `Shared.contract` and has
  `AddControllers()`/`Swashbuckle` from the template.

## Out of scope / documented follow-up

- `ScriptFlow.API` does not yet consume `PrescriptionAcknowledgedEvent`/
  `PrescriptionRejectedEvent` to move its own `Prescription.Status` to
  `Dispatched`/`Acknowledged`/`Rejected` — that requires giving `ScriptFlow.API` a
  consumer too, which is a separate pass (you scoped this session to Dispatch.Worker,
  using `ScriptFlow.API` only as a style reference).
- `Notification.Service` is untouched; it's expected to reuse the same
  `IEventConsumer<TEvent>`/DLQ pattern later.

## Implementation order

1. `Shared.contract`: `PharmacyDispatchOutcome`, `PharmacyDispatchRequest`, `PharmacyDispatchResponse`.
2. `Shared.Events`: `PrescriptionAcknowledgedEvent`, `PrescriptionRejectedEvent`.
3. `Shared.Infrastructure`: `RabbitMqConsumerSettings`, `IEventConsumer<TEvent>`,
   `RabbitMqEventConsumer`, `RabbitMqConsumerServiceCollectionExtensions`.
4. `PharmacyGateway.mock`: `PharmacyController` with the delay/ack/reject/drop simulation.
5. `Dispatch.Worker/Application`: interfaces, `PrescriptionSignedEventHandler`.
6. `Dispatch.Worker/Infrastructure`: `PharmacyGatewayHttpClient` + Polly policy,
   `InMemoryProcessedMessageStore`, `DispatchWorkerServiceCollectionExtensions`.
7. `Dispatch.Worker`: `Worker.cs`, `Program.cs`, `appsettings.json` sections.
8. `SPEC/DatabaseSpec.md`: add `ProcessedMessages` table.
9. `dotnet build ScriptFlow.sln` and fix any compile errors.

## Verification

- `dotnet build ScriptFlow.sln` succeeds with no errors.
- Start a local RabbitMQ broker, then run `PharmacyGateway.mock`, `Dispatch.Worker`, and
  `ScriptFlow.API` together. Register/login, create a patient/provider/prescription, sign
  it via Swagger — confirm `Dispatch.Worker`'s console logs show: message consumed with a
  correlation ID matching the one logged by `ScriptFlow.API`'s sign request, a pharmacy
  call, and either an Acknowledged/Rejected outcome log or (on repeated drops) retry
  attempts followed by a dead-lettered message.
- Inspect the RabbitMQ management UI (or `rabbitmqadmin`) to confirm
  `dispatch.prescription-signed` and its `.dlq` queues exist and that messages land in the
  DLQ only after exhausting retries.
- Re-publish/redeliver the same `PrescriptionSignedEvent` (or requeue a DLQ message) and
  confirm the idempotency check logs "already processed" and skips a second pharmacy call.
- If no local RabbitMQ broker is running, confirm `Dispatch.Worker` still starts (matching
  `RabbitMqEventPublisher`'s existing resilient-connection behavior) and logs a warning
  rather than crashing.
