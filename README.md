# ScriptFlow

[![CI](https://github.com/Khayyam-Adeel/ScriptFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/Khayyam-Adeel/ScriptFlow/actions/workflows/ci.yml)

ScriptFlow is an electronic-prescribing platform: prescribers create and sign
prescriptions, the system dispatches them to a (simulated) pharmacy gateway that is
deliberately slow and unreliable, and prescribers watch the outcome update live. The
system is a small set of cooperating .NET 8 services connected by RabbitMQ, plus an
Angular 18 frontend.

For the full problem statement and requirements, see [`SPEC/MainSpec.md`](SPEC/MainSpec.md).
For a detailed, as-implemented reference of every project, endpoint, and config value, see
[`SystemReference.md`](SystemReference.md) — this README covers what you need to get
running and the reasoning behind the biggest design decisions.

## 1. Architecture overview

**Core flow:** a doctor creates a prescription in `ScriptFlow.API`, signs it (publishing
`PrescriptionSignedEvent`), `Dispatch.Worker` consumes that event and calls
`PharmacyGateway.mock`, the outcome flows back through `ScriptFlow.API` as a status
update, and `Notification.Service` relays that status change to the browser over
SignalR in real time.

```
ScriptFlow-UI (Angular)
      │  REST + SignalR
      ▼
ScriptFlow.API ───publishes PrescriptionSignedEvent───▶ RabbitMQ ──▶ Dispatch.Worker
      ▲                                                                    │
      │                                                                    ▼
      │  PrescriptionAcknowledged/RejectedEvent (via RabbitMQ)   PharmacyGateway.mock
      └───────────────────────────────────────────────────────── (slow, unreliable)
      │
      ▼  PrescriptionStatusChangedEvent (via RabbitMQ)
Notification.Service ──SignalR──▶ browser (live status board)
```

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

`ScriptFlow.API` itself is layered `Controller → Application → Domain → Infrastructure`
(CQRS via MediatR, FluentValidation, Dapper-backed SQL Server repositories). See
[`SystemReference.md §3`](SystemReference.md#3-scriptflowapi) for the full breakdown.

## 2. Prerequisites

- .NET 8 SDK
- Node.js 20+ and npm (for the Angular frontend)
- SQL Server reachable locally (or update the connection string)
- RabbitMQ reachable locally (or update the `RabbitMq` config section)
- Docker Desktop — **optional**. The repo ships Dockerfiles and a
  `docker-compose.yml` for every service (see §4.2), but they have not been run
  locally, since Docker Desktop isn't installed on our office Windows machines
  under current IT policy. See [ADR-004](#adr-004-ship-docker-support-without-being-able-to-run-it-locally).

## 3. Repository layout

```
Backend/
  ScriptFlow.API/          Prescription API (see SPEC/ApiSpec.md)
  Dispatch.Worker/         Consumes signed prescriptions, calls the pharmacy gateway
  PharmacyGateway.mock/    Simulated third-party pharmacy
  Notification.Service/    SignalR relay for live status updates
  Shared.contract/         Cross-service enums + HTTP contract DTOs
  Shared.Events/           RabbitMQ integration event DTOs
  Shared.Infrastructure/   Correlation IDs, Serilog, RabbitMQ pub/sub, idempotency
Frontend/
  ScriptFlow-UI/           Angular 18 SPA
SPEC/                      Source-of-truth requirements docs
docker-compose.yml         Full-stack orchestration (untested locally, see ADR-004)
ScriptFlow.sln
```

## 4. Setup

### 4.1 Native (dotnet / npm) — the path actually used and verified day to day

1. Have SQL Server and RabbitMQ reachable locally, or update
   `Backend/ScriptFlow.API/appsettings.Development.json` (`ConnectionStrings:ScriptFlowDb`)
   and each service's `RabbitMq` section to point elsewhere.
2. Build the solution:
   ```
   dotnet build ScriptFlow.sln
   ```
3. Run each backend service (order doesn't matter — `RabbitMqEventPublisher`
   tolerates the broker being briefly unavailable):
   ```
   dotnet run --project Backend/PharmacyGateway.mock
   dotnet run --project Backend/Dispatch.Worker
   dotnet run --project Backend/Notification.Service
   dotnet run --project Backend/ScriptFlow.API
   ```
4. `ScriptFlow.API` serves Swagger at `/swagger` in Development, with a JWT
   "Authorize" button.
5. Run the frontend:
   ```
   cd Frontend/ScriptFlow-UI
   npm install
   npm start
   ```
   Angular CLI dev server on `http://localhost:4200`.

### 4.2 Docker-ready (prepared for future use — see ADR-004)

Every backend service has a `Dockerfile` (build context is the **repo root**, since
each project references the `Shared.*` class libraries next to it), and the frontend
has its own `Dockerfile` (build context is `Frontend/ScriptFlow-UI`). A root
`docker-compose.yml` wires all five services together with SQL Server and RabbitMQ
containers for a one-command environment:

```
docker compose up --build
```

Build a single image directly:
```
docker build -f Backend/ScriptFlow.API/Dockerfile -t scriptflow-api .
```

This path is **not yet exercised** in this environment (no local Docker runtime
available) — treat `docker-compose.yml`'s passwords/ports/env vars as a documented
starting point to validate once Docker is available, not a proven config.

### 4.3 Smoke test (either path)

Register → login → create a patient → create a provider (needs a seeded practice
location) → create a prescription → sign it → watch `Dispatch.Worker`'s logs for the
pharmacy call and outcome → watch the dashboard's status board update live via
SignalR. Confirm 400/401/404/409 responses for invalid input, missing token, unknown
patient, and double-signing respectively.

## 5. Configuration reference

Every service uses the same `Jwt` (`Issuer`/`Audience`/`SigningKey`/`ExpiryMinutes` —
**must match exactly** between `ScriptFlow.API` and `Notification.Service`) and
`RabbitMq` (`HostName`/`Port`/`UserName`/`Password`/`ExchangeName`) config shape via
`appsettings.json`.

| Setting | Where | Notes |
|---|---|---|
| `ConnectionStrings:ScriptFlowDb` | `ScriptFlow.API/appsettings.Development.json` | SQL Server connection string; only present in the Development file |
| `Jwt:*` | `ScriptFlow.API`, `Notification.Service` | Dev signing key is a placeholder (`CHANGE_ME_...`) — replace for anything beyond local dev |
| `RabbitMq:*` | All four backend services | Defaults to `localhost:5672`, `guest`/`guest`, exchange `scriptflow.events` |
| `PharmacyGateway:BaseUrl` / `TimeoutSeconds` | `Dispatch.Worker/appsettings.json` | Points at `PharmacyGateway.mock`'s local URL |
| `Cors:AllowedOrigins` | `ScriptFlow.API`, `Notification.Service` | Must include the Angular dev server origin (`http://localhost:4200`) |

## 6. Architecture Decision Records

### ADR-001: Repository interfaces over Domain/Application, SQL Server + Dapper behind them

**Status:** Accepted

**Context:** `ScriptFlow.API` needed persistence for Prescriptions, Patients,
Providers, Practices, and Users. `SPEC/DatabaseSpec.md` was empty when the API was
first built, so the real schema wasn't known yet, but the Application layer still
needed something to code against.

**Decision:** Define repository interfaces (`IPrescriptionRepository`,
`IPatientRepository`, etc.) in the Application layer, per clean-architecture
dependency direction (Application depends on abstractions, not concrete storage).
The first implementation was in-memory (`ConcurrentDictionary`-backed singletons);
once `DatabaseSpec.md` was written, those were swapped for SQL Server-backed
repositories that call stored procedures via Dapper, with no changes needed above
the Infrastructure layer.

**Rejected alternatives:**
- **EF Core from the start** — would have required designing (and likely
  re-designing) a full schema before the database spec existed, blocking all other
  layers on a decision that wasn't ready to make. Repository interfaces let
  Domain/Application/Api be built and tested against in-memory stores immediately.
- **Dapper without stored procedures (inline SQL)** — rejected in favor of stored
  procedures because the project's SQL conventions (`Skills/BackendSkill.md`)
  standardize on schema-qualified stored procedures with `TRY/CATCH` + error
  logging (`dbo.TblErrorLog`) — inline SQL in C# would bypass that convention and
  scatter data-access logic across two layers instead of one.
- **Full EF Core + ASP.NET Identity for auth** — rejected in favor of a minimal
  `PasswordHasher<User>` wrapper + hand-rolled JWT issuance, since the spec only
  needed register/login, not the rest of Identity's surface (roles, external
  logins, lockout policies) — pulling in the whole framework would have added
  configuration surface with no corresponding requirement.

**Consequence:** persistence swaps are isolated to `Infrastructure/Persistence/`;
Domain and Application never changed when the backing store did. Documented
limitation carried during the in-memory phase: data didn't survive restarts — now
resolved by the SQL Server repositories.

### ADR-002: RabbitMQ topic exchange with per-consumer dead-letter queues for the dispatch pipeline

**Status:** Accepted

**Context:** `Dispatch.Worker` must consume `PrescriptionSignedEvent`, call an
unreliable pharmacy gateway, and reliably report the outcome back — without losing
messages on transient failure, and without a poison message blocking the queue
forever. The same consume/ack/dead-letter shape is also needed later by
`ScriptFlow.API`'s outcome handlers and by `Notification.Service`.

**Decision:** A single shared topic exchange (`scriptflow.events`) carries every
integration event, routed by event-type name. Each consumer declares its own queue
bound to that exchange, plus a matching dead-letter exchange/queue
(`x-dead-letter-exchange` on the main queue). On handler failure, the consumer
`Nack`s with `requeue:false`, and RabbitMQ automatically routes the message to that
consumer's DLQ — no custom retry-topology code needed. This plumbing
(`IEventConsumer<T>`, `RabbitMqEventConsumer`, `RabbitMqConsumerSettings`) was built
once in `Shared.Infrastructure` rather than inside `Dispatch.Worker`, specifically so
`ScriptFlow.API` and `Notification.Service` could reuse it verbatim.

**Rejected alternatives:**
- **A direct exchange per event type, hand-rolled per service** — rejected because
  it would duplicate the exchange/queue/DLQ declaration and QoS/ack logic in three
  services instead of writing it once; a topic exchange with routing keys gives the
  same event-type separation with one exchange to manage.
- **Application-level retry loop instead of DLQ** — rejected for the "poison
  message" case (a message that will never succeed, e.g. malformed payload):
  looping in-process risks blocking the consumer thread indefinitely or losing the
  message on a crash. Dead-lettering makes a stuck message visible and inspectable
  in the RabbitMQ management UI instead of silently retried forever or dropped.
- **A message broker-level retry count/TTL requeue trick** (republish to the same
  queue with a delay) — rejected in favor of pairing RabbitMQ's DLQ with an
  in-process retry policy (see ADR-003) at the HTTP-call layer, so "retry a
  transient HTTP failure" and "give up and quarantine a poison message" stay two
  clearly separate concerns instead of one overloaded queue-requeue mechanism.

**Consequence:** a new consumer for a new event type is a few lines of DI
registration (`AddRabbitMqConsumer<TEvent>(settings)`), not new plumbing. Messages
that exhaust retries are quarantined in a DLQ per consumer, inspectable without
being lost.

### ADR-003: Polly retry policy at the HTTP-client layer, not a hand-rolled retry loop

**Status:** Accepted

**Context:** `PharmacyGateway.mock` deliberately drops ~20% of requests (severs the
connection via `HttpContext.Abort()`) to simulate an unreliable upstream partner.
`Dispatch.Worker` needs to absorb that transient failure without treating every drop
as a permanent, dead-letter-worthy failure.

**Decision:** Attach a Polly retry policy to the typed `HttpClient` for
`IPharmacyGatewayClient` via `AddPolicyHandler` (3 retries, exponential backoff:
2s/4s/8s, triggered on `HttpRequestException`/timeout). A **Rejected** response
(e.g. out of stock) is treated as a legitimate business outcome and is never
retried — only **Drop** (severed connection) is transient failure Polly retries
against. Only once retries are exhausted does the message dead-letter (ADR-002).

**Rejected alternatives:**
- **Hand-rolled retry loop around the HTTP call** — rejected because Polly already
  solves backoff, jitter, and "which exceptions count as transient" correctly and
  is a single declarative policy attached at DI-registration time, versus
  re-implementing (and re-testing) the same logic by hand in
  `PrescriptionSignedEventHandler`.
- **Retrying "Rejected" responses the same as "Drop"** — rejected because a
  pharmacy rejection (out of stock) is a real, final answer from the business
  process, not a transient fault; retrying it would just re-ask the same question
  and get the same answer, and would incorrectly delay a status the user is
  already waiting on.
- **RabbitMQ-level requeue-with-delay instead of an HTTP-layer retry** — rejected
  per ADR-002: it would conflate "the HTTP call to the pharmacy failed transiently"
  with "the message itself is unprocessable," making the two failure modes harder
  to reason about and monitor separately.

**Consequence:** the mock gateway's simulated instability is absorbed
transparently for the ~20%-drop case without extra code in the handler; only
genuinely exhausted failures reach the DLQ.

### ADR-004: Ship Docker support without being able to run it locally

**Status:** Accepted

**Context:** Docker Desktop is not installed on our office Windows machines due to
current IT policy, and there's no near-term path to get it installed. The
application still needs to be deployable to any Docker-enabled environment (e.g. a
CI/CD pipeline, a colleague's machine, or a hosting platform) without requiring
significant rework later.

**Decision:** Add a multi-stage `Dockerfile` for every backend service and the
Angular frontend, plus a root `docker-compose.yml` wiring the full stack (including
SQL Server and RabbitMQ containers) together, now — while the app is otherwise still
being built out — rather than deferring containerization to whenever Docker access
becomes available. These files are written using standard, well-established
patterns (the same shape `dotnet publish` / official ASP.NET Core Docker guidance
produces) precisely because they can't be locally verified: minimizing novel or
environment-specific Docker behavior reduces the risk that they're wrong in ways
that would only surface once Docker finally is available.

**Rejected alternatives:**
- **Wait until Docker Desktop is available to write any Docker config** —
  rejected because it risks discovering containerization problems (missing
  project references in the build context, port mismatches, config that assumed
  `localhost`) only after the rest of the app has moved on, when they're more
  expensive to fix. Writing it alongside the app keeps the Dockerfiles in sync with
  each project's actual shape as it evolves.
- **A single monolithic Dockerfile/image for the whole backend** — rejected in
  favor of one Dockerfile per service, matching the existing multi-service
  architecture (§1); a monolithic image would contradict the whole reason the
  system is split into independently deployable services (`ScriptFlow.API`,
  `Dispatch.Worker`, `Notification.Service`, `PharmacyGateway.mock`).
- **Podman or another rootless/IT-policy-friendlier container runtime as a
  workaround** — considered but not pursued in this pass, since the immediate goal
  was to keep the *application* container-ready and standards-based (plain
  Dockerfiles + Compose), not to solve the local tooling restriction itself; a
  standard Dockerfile/Compose setup runs unmodified under Podman later if that
  becomes the chosen workaround.

**Consequence:** containerization is "shovel-ready" but explicitly **unverified**
in this repo — `docker-compose.yml` carries a comment flagging this, and anyone
picking it up on a Docker-enabled machine should treat first run as a validation
pass (build errors, port conflicts, and connection-string/env-var mismatches are
the most likely issues), not an already-proven deployment path.

**Update:** the underlying reason `docker-compose.yml` remains unverified narrowed
since this ADR was written — Docker Desktop is now installed on this machine, but
its daemon can't start because WSL2 itself isn't installed (`wsl --install` needs
admin rights and a restart, neither available here). What *did* become verifiable
without Docker is the database bootstrap `docker-compose.yml`'s `db-init` service
runs (`Infrastructure/Database/Schema/00_CreateSchema.sql`,
`01_SeedSystemUser.sql`, every `StoredProcedures/*.sql`, then
`Performance/01_ExpandLookupData.sql`) — each was run in that exact order against a
real, brand-new SQL Server database (not a container, but the same SQL Server
engine) and produced a working, fully-seeded schema. So the piece most likely to
be wrong (the SQL itself) is now confirmed correct; what's still unverified is
narrower - purely the container orchestration around it (healthchecks, service
dependency ordering, container networking).

## 7. Security

See [`SECURITY.md`](SECURITY.md) for the security baseline (authentication,
authorization, input validation, parameterized data access, secrets handling) and a
short OWASP Top 10 (2021) self-assessment, written against the actual code with file
references — including the gaps that are still open.

## 8. Performance

See [`PERFORMANCE.md`](PERFORMANCE.md) for the performance chapter: a 1,050,000-row
prescription seed in the real local SQL Server instance, three reporting queries, and
measured (not estimated) before/after execution plans and `STATISTICS IO/TIME` for
each — including one index that was added but honestly reported as **not** improving
its target query, with the measured reason why.
