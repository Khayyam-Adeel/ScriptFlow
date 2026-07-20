# ScriptFlow — System Reference

> **Type:** Reference (Diátaxis) — an information-oriented map of what exists and how it fits
> together. For *why* decisions were made, see the `Why:` notes throughout and the source
> spec/plan docs linked from each section. For step-by-step setup, see [Getting Started](#9-getting-started).

## 1. Overview

ScriptFlow is an electronic-prescribing platform: prescribers create and sign prescriptions,
the system dispatches them to a (simulated) pharmacy gateway that is deliberately slow and
unreliable, and prescribers watch the outcome update live. The system is built as a small set
of cooperating .NET services connected by RabbitMQ, plus an Angular frontend.

The full problem statement, functional/non-functional requirements, and prescription lifecycle
are defined in [`SPEC/MainSpec.md`](SPEC/MainSpec.md) — treat it as the source of truth for
*intent*; this document describes the system *as implemented*.

**Core flow:** a doctor creates a prescription in `ScriptFlow.API`, signs it (publishing
`PrescriptionSignedEvent`), `Dispatch.Worker` consumes that event and calls
`PharmacyGateway.mock`, the outcome flows back through `ScriptFlow.API` as a status update, and
`Notification.Service` relays that status change to the browser over SignalR in real time.

## 2. Solution Map

| Project | Path | Kind | Purpose |
|---|---|---|---|
| `ScriptFlow.API` | `Backend/ScriptFlow.API` | ASP.NET Core Web API | Prescription/patient/provider/practice CRUD, auth, lifecycle state machine, event publishing |
| `Dispatch.Worker` | `Backend/Dispatch.Worker` | .NET Worker Service | Consumes signed prescriptions, calls the pharmacy gateway, publishes the outcome |
| `PharmacyGateway.mock` | `Backend/PharmacyGateway.mock` | ASP.NET Core Web API | Simulates a slow, unreliable third-party pharmacy (random delay/ack/reject/drop) |
| `Notification.Service` | `Backend/Notification.Service` | ASP.NET Core + SignalR | Relays prescription status changes to connected browsers in real time |
| `Shared.contract` | `Backend/Shared.contract` | Class library | Dependency-free enums and cross-service HTTP contract DTOs |
| `Shared.Events` | `Backend/Shared.Events` | Class library | Integration event DTOs published/consumed over RabbitMQ |
| `Shared.Infrastructure` | `Backend/Shared.Infrastructure` | Class library | Cross-cutting plumbing: correlation IDs, Serilog bootstrap, RabbitMQ pub/sub, idempotency |
| `ScriptFlow-UI` | `Frontend/ScriptFlow-UI` | Angular 18 app | Login/register, patient/provider/prescription management, live status board |

All backend projects target **.NET 8** and are collected in `ScriptFlow.sln` at the repo root.

## 3. ScriptFlow.API

Layered per `Controller → Application → Domain → Infrastructure`, matching
[`SPEC/ApiSpec.md`](SPEC/ApiSpec.md). Folder names are kept exactly as scaffolded (`APi`, not
`Api`).

### 3.1 Domain (`Domain/`)

- **Entities:** `Prescription` (aggregate root), `PrescriptionMedication`, `Patient`,
  `Provider`, `Practice`, `PracticeLocation`, `Medicine`, `User` (the login identity — separate
  from a `Provider` profile).
- **Value objects** (`ValueObjects/`), each validating its format in the constructor and
  throwing `DomainException` otherwise:
  - `Scid` — a prescription's own identifier: `9` + 5 alphanumeric chars (EPS entity no) + 5
    more alphanumeric chars (e.g. `9J0BGVA1B2C`). Distinct from a medicine's SNOMED CT code.
  - `Nhi` — NZ National Health Index number, `[A-Z]{3}[0-9]{4}` (e.g. `ABC1234`).
  - `HpiNumber` — a practice location's Health Point Identifier, composed of `HpiNo`
    (`[A-Z]{3}[0-9]{2}`) and a single-letter `HpiExtension`, displayed as `FZZ99-B`.
- **`Prescription` state machine:** `Created → Signed → Dispatched → Acknowledged | Rejected`,
  with `Expired` reachable from any non-terminal state (enum: `Shared.contract.PrescriptionStatus`).
  `Sign()`, `Acknowledge()`, `Reject()`, and `Repeat()` each enforce legal source states and throw
  `InvalidPrescriptionStateException` otherwise. **`ScriptFlow.API` itself only drives
  `Created → Signed`**; the `Acknowledged`/`Rejected` transitions are applied by this API's own
  event handlers (§3.4) reacting to what `Dispatch.Worker` reports back.
- `Repeat()` copies the medication list into a new `Prescription` (status `Created`) with
  `RepeatOfPrescriptionId` set; only legal when the source is `Signed`, `Dispatched`, or
  `Acknowledged`.

### 3.2 Application (`Application/`)

CQRS via **MediatR**, with **FluentValidation** run as a pipeline behavior:

- `Commands/` + `Handlers/`: `CreatePrescriptionCommand`, `UpdatePrescriptionCommand`,
  `SignPrescriptionCommand`, `RepeatPrescriptionCommand`, `CreatePatientCommand`,
  `CreateProviderCommand`, `RegisterUserCommand`, `LoginCommand` — one handler class per command.
- `Queries/` + `Handlers/`: `GetPrescriptionByIdQuery`, `ListPrescriptionsQuery`,
  `GetPatientByIdQuery`, `SearchPatientsQuery`, `GetProviderByIdQuery`, plus list queries for
  medicines/practices/practice locations/providers (reference-data lookups for the UI).
- `Validators/`: one `AbstractValidator<T>` per command, encoding the field rules from
  `ApiSpec.md`'s "PreRequisites" section.
- `Behaviors/`: `ValidationBehavior<TRequest,TResponse>` (runs validators, throws on failure →
  mapped to 400) and `LoggingBehavior<TRequest,TResponse>` (logs request name, correlation ID,
  duration, and outcome around every command/query, giving a full per-request call trace).
- `Mappings/MappingExtensions.cs`: hand-written `ToDto()` extension methods, no AutoMapper.
- `Handlers/PrescriptionAcknowledgedEventHandler.cs` and `PrescriptionRejectedEventHandler.cs`:
  **not** MediatR handlers — these consume `PrescriptionAcknowledgedEvent` /
  `PrescriptionRejectedEvent` off RabbitMQ (published by `Dispatch.Worker`), transition the
  matching `Prescription` via `Acknowledge()`/`Reject()`, persist it, and republish
  `PrescriptionStatusChangedEvent` for `Notification.Service` to relay to the browser.

### 3.3 Infrastructure (`Infrastructure/`)

- **Persistence** (`Persistence/`): SQL Server-backed repositories (`SqlPrescriptionRepository`,
  `SqlPatientRepository`, `SqlProviderRepository`, `SqlMedicineRepository`,
  `SqlPracticeRepository`, `SqlPracticeLocationRepository`, `SqlUserRepository`), each
  implementing an `Application/Interfaces` contract and calling stored procedures via **Dapper**
  (`Database/ISqlConnectionFactory` / `SqlConnectionFactory`). The stored procedures themselves
  live under `Infrastructure/Database/StoredProcedures/<Schema>/` (see §8) and are tracked in
  git alongside the schema script — `SPEC/DatabaseSpec.md` documents the same schema in prose
  form for quick reference. The project previously used in-memory
  repositories (`InMemory*Repository`) seeded at startup; those have been removed in favor of
  the SQL-backed implementations.
- **Auth** (`Auth/`): `JwtTokenGenerator` (issues bearer tokens), `PasswordHasher` (wraps
  `Microsoft.AspNetCore.Identity.PasswordHasher<User>`), `JwtOptions` (bound from `appsettings.json`).

### 3.4 API layer (`APi/Controllers/`)

All endpoints return JSON; `PrescriptionsController` is `[Authorize]`-protected.

| Controller | Endpoints |
|---|---|
| `AuthController` | `POST /api/auth/register`, `POST /api/auth/login` → JWT |
| `PatientsController` | create, get-by-id, search (by name/NHI) |
| `ProvidersController` | create, get-by-id, list |
| `PracticesController` | create, get-by-id, list |
| `PracticeLocationsController` | create, get-by-id, list |
| `MedicinesController` | get-by-id, list |
| `PrescriptionsController` | `POST /api/prescriptions` (201), `PUT /{id}` (update, only while `Created`), `POST /{id}/sign` (200, publishes `PrescriptionSignedEvent`), `POST /{id}/repeat` (201, publishes `PrescriptionRepeatedEvent`), `GET /{id}`, `GET ?patientId=&status=` |

`APi/Middleware/ExceptionHandlingMiddleware` maps `ValidationException`→400,
`UnauthorizedAccessException`→401, `EntityNotFoundException`→404,
`InvalidPrescriptionStateException`→409, everything else→500, as a consistent problem-details
JSON body.

`Program.cs` composition root wires (in order): Serilog bootstrap (`Shared.Infrastructure`),
MediatR + FluentValidation + logging pipeline (`AddApplication()`), SQL repositories + JWT +
password hashing (`AddInfrastructure()`), correlation ID + RabbitMQ publisher
(`AddSharedInfrastructure()`), JWT bearer auth, CORS (allowing the Angular dev server origin),
Swagger with a bearer security scheme, and the outcome-event consumers from §3.2.

## 4. Dispatch.Worker & PharmacyGateway.mock

Together these implement `SPEC` line item "a Dispatch worker... integrating with a mock
pharmacy gateway you also build — it must randomly delay, ack, nack, and drop messages."
Design rationale is captured in
[`Backend/Dispatch.Worker/IMPLEMENTATION_PLAN.md`](Backend/Dispatch.Worker/IMPLEMENTATION_PLAN.md);
requirements in [`Backend/Dispatch.Worker/DeliveryServiceSpec.md`](Backend/Dispatch.Worker/DeliveryServiceSpec.md)
(this file was moved out of `SPEC/` into the worker's own folder; it is tracked in git there).

**Flow:**

```
ScriptFlow.API                Dispatch.Worker                     PharmacyGateway.mock
───────────────                ───────────────                     ─────────────────────
SignPrescriptionCommandHandler
  publishes PrescriptionSignedEvent
        │
        ▼  [scriptflow.events] topic exchange, routing key "PrescriptionSignedEvent"
  [dispatch.prescription-signed] queue ──▶ Worker.cs (BackgroundService)
                                              │
                                              ▼
                                   PrescriptionSignedEventHandler
                                     1. already processed? → skip (idempotency)
                                     2. IPharmacyGatewayClient.DispatchAsync(...) ──▶ POST /api/pharmacy/dispatch
                                        (Polly: 3 retries, 2s/4s/8s backoff)              │ random 100-2000ms delay,
                                              │◀── Acknowledged / Rejected ────────────────┘ then Ack (~60%) /
                                              ▼                                              Reject (~20%) /
                                   publish PrescriptionAcknowledgedEvent                      Drop via HttpContext.Abort() (~20%)
                                     or PrescriptionRejectedEvent
                                              │ (retries exhausted → exception propagates)
                                              ▼
                                   consumer Nacks(requeue:false) ──▶ [dispatch.prescription-signed.dlq]
```

Two outcomes are deliberately different code paths: **Rejected** is a legitimate business
answer (e.g. out of stock) — not retried, always published as `PrescriptionRejectedEvent`.
**Drop** is a transient failure — Polly retries it 3 times (exponential backoff); only once
retries are exhausted does the message dead-letter.

- `PharmacyGateway.mock/Controllers/PharmacyController.cs` — the entire simulation: delay, then
  a weighted random roll (20% drop / 20% reject / 60% ack).
- `Dispatch.Worker/Worker.cs` — deliberately thin: `await consumer.ConsumeAsync(handler.HandleAsync, ct)`.
- `Dispatch.Worker/Application/Handlers/PrescriptionSignedEventHandler.cs` — the orchestration
  logic (idempotency check → pharmacy call → publish outcome → mark processed).
- Idempotency and retry/backoff/DLQ plumbing are shared infrastructure — see §6.

## 5. Notification.Service

Purpose: relay `PrescriptionStatusChangedEvent` (published by `ScriptFlow.API` once a status
write lands in SQL Server, see §3.2) to every connected browser over SignalR, so the UI's status
board updates live without polling.

- `Hubs/PrescriptionHub.cs` — server-push only hub (`[Authorize]`); clients never call methods
  on it, they just connect and listen.
- `Application/PrescriptionStatusChangedEventHandler.cs` — consumes the event, broadcasts a
  `prescriptionStatusChanged` message to `Clients.All`. No DB access, no idempotency store — a
  duplicate broadcast is harmless (the client re-applies the same status).
- `Program.cs` maps the hub at `/hubs/prescriptions` and validates the same JWT
  issuer/audience/signing key as `ScriptFlow.API` (must be kept in sync via each project's
  `appsettings.json` `Jwt` section). Since browsers can't set an `Authorization` header on a
  WebSocket handshake, the token is passed as an `access_token` query-string parameter and
  pulled back out for JWT validation in `Program.cs`'s `OnMessageReceived` handler.
- `NotificatioServiceSpec.md` in `SPEC/` is currently empty — no separate requirements doc for
  this service beyond what's implied by `MainSpec.md`'s "live status board" requirement.

## 6. Shared Libraries

| Library | Contents | Used by |
|---|---|---|
| `Shared.contract` | `Enums/PrescriptionStatus`, `Enums/ProviderType`, `Enums/PharmacyDispatchOutcome`; `Contracts/PharmacyDispatchRequest`, `Contracts/PharmacyDispatchResponse` (the HTTP contract between `Dispatch.Worker` and `PharmacyGateway.mock`) | All four backend services |
| `Shared.Events` | `IntegrationEvent` base (`EventId`, `OccurredAtUtc`, `CorrelationId`) + `PrescriptionCreatedEvent`, `PrescriptionSignedEvent`, `PrescriptionRepeatedEvent`, `PrescriptionAcknowledgedEvent`, `PrescriptionRejectedEvent`, `PrescriptionStatusChangedEvent` | `ScriptFlow.API`, `Dispatch.Worker`, `Notification.Service` |
| `Shared.Infrastructure` | `Correlation/` (`ICorrelationIdAccessor`, `CorrelationIdMiddleware` — threads one correlation ID from an HTTP request through to every downstream log line and published event); `Logging/SerilogExtensions` (identical Serilog bootstrap for every service, tagging each log line with a `Service` property); `Messaging/` (`IEventPublisher`/`RabbitMqEventPublisher`, `IEventConsumer<T>`/`RabbitMqEventConsumer`, `RabbitMqConsumerSettings`, `EventConsumerBackgroundService<T>` — declares a shared topic exchange, a per-consumer queue + dead-letter queue/exchange, sets QoS, and on failure `Nack`s with `requeue:false` so RabbitMQ auto-routes to the DLQ); `Idempotency/` (`IProcessedMessageStore`/`InMemoryProcessedMessageStore` — documented limitation: resets on process restart, a durable `ProcessedMessages` SQL table is designed in `DatabaseSpec.md` as the future replacement) | All four backend services |

Every event consumer across the system (`Dispatch.Worker`, `ScriptFlow.API`'s outcome handlers,
`Notification.Service`) follows the same shape: `AddRabbitMqConsumer<TEvent>(settings)` +
`AddHostedService(provider => new EventConsumerBackgroundService<TEvent>(...))`, so a new
consumer for a new event type is a few lines of DI registration, not new plumbing.

## 7. Frontend — ScriptFlow-UI

Angular 18 SPA in `Frontend/ScriptFlow-UI`, using the Angular CLI's standard structure
(standalone components, no NgModules):

- `core/` — `services/` (one per backend resource: `auth`, `patient`, `provider`, `practice`,
  `practice-location`, `medicine`, `prescription`, plus `prescription-hub.service.ts` wrapping
  `@microsoft/signalr` for the live status board), `models/` (TS interfaces mirroring the API
  DTOs), `interceptors/` (`auth` — attaches the JWT bearer token; `error` — maps API
  problem-details responses), `guards/auth.guard.ts`.
- `shared/components/` — reusable presentational components: `text-field`, `select-field`,
  `button`, `status-badge` (renders `PrescriptionStatus`), `spinner`, `toast`.
- `layouts/app-shell/` — the authenticated app frame.
- `features/` — one folder per screen area: `auth` (login/register), `patients` (search, form,
  detail), `providers` (form, detail), `prescriptions` (list, form, detail — the create/sign/
  repeat workflow), `dashboard` (the live status board, consuming `prescription-hub.service.ts`).
- `app.routes.ts` / `app.config.ts` — routing and app-level providers (HTTP client with
  interceptors, etc).

Talks to `ScriptFlow.API` over REST (CORS-allowed origin `http://localhost:4200`, configured on
the API side) and to `Notification.Service` over a SignalR WebSocket at `/hubs/prescriptions`
for live status updates. UI look-and-feel intent (medical color palette, glassmorphism/minimalist
aesthetic, no CSS framework) is recorded in [`Frontend/FrontEndSpec.md`](Frontend/FrontEndSpec.md).

## 8. Specs & Planning Docs

These aren't implementation — they're the intent and decision record behind it. Read them when
you need to know *why* something is shaped the way it is, not just *what* it is.

| Doc | Governs |
|---|---|
| `SPEC/MainSpec.md` | Overall problem statement, the 3-service architecture requirement, the prescription state machine, the 1M-row performance chapter (not yet built), acceptance criteria |
| `SPEC/ApiSpec.md` | `ScriptFlow.API` scope, functional requirements (FR-001..005), non-functional requirements, error-code mapping |
| `SPEC/DatabaseSpec.md` | Full SQL Server schema: table DDL, schemas (`Profile`, `Medication`/`Lookup`, `Admin`, `Prescription`), audit-column conventions, FKs/constraints/indexes — see §3.3 for how this maps to the (untracked) stored procedures |
| `SPEC/NotificatioServiceSpec.md` | Empty — no dedicated spec beyond `MainSpec.md` |
| `Backend/Dispatch.Worker/DeliveryServiceSpec.md` | `Dispatch.Worker` requirements (consume, dispatch, retry, DLQ, idempotency, publish outcome) |
| `Backend/*/IMPLEMENTATION_PLAN.md` | Design/implementation plans for `ScriptFlow.API` and `Dispatch.Worker`, written before each was built — includes assumptions resolved with the project owner and a verification checklist |
| `Frontend/FrontEndSpec.md` | UI/UX intent for `ScriptFlow-UI` |
| `Docs/PLAN-*.md` | Feature-addition plans (patient/provider fields, prescription print preview, admin console enrichment) — all three are now marked **Implemented** at the top; kept as the design record, not rewritten after the fact |
| `Docs/DEMO_GUIDE.md` | Speaker notes/Q&A for presenting the project |
| `Docs/AWS-DEPLOYMENT.md` | AWS EC2 deployment path (manual SSH deploy + a Terraform/GitHub Actions upgrade path) |
| `Prompts/Prompts.md` | The literal prompts used to drive each build pass (plan-mode API build, database design, dispatch worker planning, stored-procedure generation) — a log of how this codebase was actually produced |
| `Skills/BackendSkill.md` | A reusable prompt for stored-procedure generation conventions (schema-qualified naming, `TRY/CATCH` + `dbo.TblErrorLog` logging, `NOLOCK`, join-then-filter ordering) |

`Backend/ScriptFlow.API/Spec.md` and `Backend/Dispatch.Worker/Spec.md` exist but are empty —
the real content for each lives in the files above instead.

## 9. Configuration

Every service uses the same `Jwt` (`Issuer`/`Audience`/`SigningKey`/`ExpiryMinutes` —
**must match exactly** between `ScriptFlow.API` and `Notification.Service`) and `RabbitMq`
(`HostName`/`Port`/`UserName`/`Password`/`ExchangeName`) config shape via `appsettings.json`.

| Setting | Where | Notes |
|---|---|---|
| `ConnectionStrings:ScriptFlowDb` | `ScriptFlow.API/appsettings.Development.json` | SQL Server connection string; only present in the Development file |
| `Jwt:*` | `ScriptFlow.API`, `Notification.Service` | Dev signing key is a placeholder (`CHANGE_ME_...`) — replace for anything beyond local dev |
| `RabbitMq:*` | All four backend services | Defaults to `localhost:5672`, `guest`/`guest`, exchange `scriptflow.events` |
| `PharmacyGateway:BaseUrl` / `TimeoutSeconds` | `Dispatch.Worker/appsettings.json` | Points at `PharmacyGateway.mock`'s local URL |
| `Cors:AllowedOrigins` | `ScriptFlow.API`, `Notification.Service` | Must include the Angular dev server origin (`http://localhost:4200`) |

## 10. Getting Started

1. Have SQL Server and RabbitMQ reachable locally (or update the connection strings /
   `RabbitMq` section to point elsewhere).
2. `dotnet build ScriptFlow.sln` from the repo root.
3. Run `PharmacyGateway.mock`, `Dispatch.Worker`, `Notification.Service`, and `ScriptFlow.API`
   (each `dotnet run --project Backend/<Project>`) — order doesn't matter; `RabbitMqEventPublisher`
   tolerates the broker being briefly unavailable and logs a warning rather than crashing.
4. `ScriptFlow.API` serves Swagger at `/swagger` in Development, with a JWT "Authorize" button.
5. For the frontend: `cd Frontend/ScriptFlow-UI && npm install && npm start` (Angular CLI dev
   server on `http://localhost:4200`).
6. End-to-end smoke test: register → login → create a patient → create a provider (needs a
   seeded practice location) → create a prescription → sign it → watch `Dispatch.Worker`'s logs
   for the pharmacy call and outcome → watch the dashboard's status board update live via
   SignalR.
