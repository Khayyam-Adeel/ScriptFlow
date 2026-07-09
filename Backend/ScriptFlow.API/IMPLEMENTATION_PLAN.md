# ScriptFlow.API — Prescription API Implementation Plan

## Context

`SPEC/ApiSpec.md` defines the Prescription API: prescription lifecycle (create, update, sign,
repeat, retrieve), patient/provider profile creation, login/signup, and event publishing to
RabbitMQ. The solution is already scaffolded (`ScriptFlow.sln`) with empty projects:
`ScriptFlow.API` (with empty `Domain`, `Application/{Commands,DTOs,Handlers,Interfaces,Queries,
Validators}`, `Infrastructure`, `APi/Controllers` folders — folder names kept as-is), and three
empty shared class libraries (`Shared.contract`, `Shared.Events`, `Shared.Infrastructure`)
already referenced by `ScriptFlow.API.csproj`. Nothing beyond `Program.cs` + Swagger exists today.

Goal of this pass: implement the Prescription API end-to-end using Controller → Application →
Domain → Infrastructure clean architecture, per your answers to my clarifying questions:
- Shared.* projects are used for cross-service pieces (they'll be reused by Dispatch.Worker /
  Notification.Service later, per `MainSpec.md`).
- Full register/login with JWT.
- Persistence via repository interfaces backed by in-memory stores for now (no SQL Server
  wiring yet — `DatabaseSpec.md` is empty, so schema design is deferred to a later pass).
- A real RabbitMQ publisher (not a stub), but publish failures are logged and swallowed rather
  than failing the HTTP request (no transactional outbox in this pass — documented limitation).
- logging should be implemented so that application call trace can be judged and errors should be identified.

All code changes are scoped to `Backend/ScriptFlow.API`, `Backend/Shared.contract`,
`Backend/Shared.Events`, and `Backend/Shared.Infrastructure`. No other project
(`Dispatch.Worker`, `Notification.Service`, `PharmacyGateway.mock`) is touched.

## Key assumptions from ambiguous spec wording (flag for review)

Resolved with user 2026-07-09:

1. **Two distinct identifiers, distinctly named.** ApiSpec line 3 says "Prescription must have
   unique SCTID" with pattern `9` + 5-char EPS entity no (e.g. `J0BGV`) + 5 alphanumeric chars
   (11 chars total). Line 6 says Medication also carries a `SCTID`. These are two separate
   identifiers with two separate names:
   - `Prescription.Scid` (value object `Scid`): the prescription's own system-generated unique
     identifier, format `^9[A-Za-z0-9]{5}[A-Za-z0-9]{5}$`. Called **SCID**, not SCTID, to avoid
     confusion with the medicine's real SNOMED CT code.
   - `Medicine.Sctid`: the actual SNOMED CT code for the medicine master-data record — required
     non-empty string, no format constraint (it's reference data you'll seed).
2. **HPI number is two parts.** Pattern from `FZZ99-B` is the combination of **HPI No** (the
   first 5 characters, 3 letters + 2 digits, e.g. `FZZ99`) and **HPI Extension** (the single
   letter after the `-`, e.g. `B`). Modeled as value object `HpiNumber` with two components:
   `HpiNo` (`^[A-Z]{3}\d{2}$`) and `HpiExtension` (`^[A-Z]$`), formatted together as
   `{HpiNo}-{HpiExtension}` for display/storage.
3. **NHI number**: format not specified in spec; using the standard NZ NHI shape
   `^[A-Z]{3}\d{4}$` as a reasonable default.
4. **State machine**: use `MainSpec.md`'s lifecycle exclusively — `Created → Signed →
   Dispatched → Acknowledged / Rejected / Expired`, including repeats. ApiSpec FR-004's
   "created > Pending, sent or failed, and dispensed" wording is ignored per your instruction.
   The API itself only *drives* Created → Signed (and spawns repeats); the later states are
   transitioned by Dispatch.Worker in a future pass, but the enum models all of them now so the
   contract is stable across services.
5. **Practice and PracticeLocation are two separate, linked tables.** `Practice` is a minimal
   table (only the columns needed — id, name) and `PracticeLocation` has a foreign key to
   `Practice`, plus its own `HpiNumber` (HpiNo + HpiExtension). A `Provider` is linked to one
   `PracticeLocation` for this pass. Each `Prescription` captures the `ProviderId` +
   `PracticeLocationId` active at creation time, for audit purposes.
6. **Patients/Providers "create profile"**: full CRUD is deferred until actually needed — just
   create + get-by-id for now. However, **patient search is in scope for this pass**, since
   prescription creation needs to find an existing patient to prescribe against: `Patients`
   gets a search endpoint (by name and/or NHI) in addition to create/get-by-id. Providers stay
   at create + get-by-id only (no search needed yet — prescriptions are created by the
   authenticated provider, not looked up).

## Cross-project layout

- **Shared.contract** — pure enums/value types shared across services (no dependencies):
  `PrescriptionStatus`, `ProviderType`.
- **Shared.Events** — integration event DTOs published to RabbitMQ: `PrescriptionCreatedEvent`,
  `PrescriptionSignedEvent`, `PrescriptionRepeatedEvent`, base `IntegrationEvent` (Id,
  OccurredAtUtc, CorrelationId). References `Shared.contract` for the status enum.
- **Shared.Infrastructure** — cross-cutting plumbing reusable by every service:
  `IEventPublisher` + `RabbitMqEventPublisher` (+ `RabbitMqOptions`), correlation-ID
  middleware + `ICorrelationIdAccessor`, a Serilog bootstrap extension
  (`SerilogExtensions.AddStructuredLogging`). Takes `RabbitMQ.Client`, `Serilog.AspNetCore`.
- **ScriptFlow.API** — everything else (Domain/Application/Infrastructure/Api), per clean
  architecture, described below.

## Domain layer (`Backend/ScriptFlow.API/Domain`)

- `Entities/`: `Prescription` (aggregate root), `PrescriptionMedication`, `Medicine`, `Patient`,
  `Provider`, `Practice`, `PracticeLocation` (FK `PracticeId` → `Practice`), `User`.
- `ValueObjects/`: `Scid` (prescription identifier), `Nhi`, `HpiNumber` (composed of `HpiNo` +
  `HpiExtension`) — each validates its regex in the constructor and throws `DomainException` on
  invalid format.
- `Exceptions/`: `DomainException` (base), `InvalidPrescriptionStateException` (→ 409),
  `EntityNotFoundException` (→ 404).
- `Prescription` state machine (enum from `Shared.contract.PrescriptionStatus`):
  `Created → Signed`, `Signed → Dispatched → Acknowledged|Rejected`, `* → Expired`. Methods
  `Sign()`, `Repeat()` enforce legal transitions and raise domain events internally (returned to
  the caller, not dispatched from inside the entity, to keep Domain free of infrastructure
  concerns).
- `Prescription.Repeat()` creates a new `Prescription` in `Created` status referencing
  `RepeatOfPrescriptionId`, copying medications; only legal when the source is `Signed`,
  `Dispatched`, or `Acknowledged`.

## Application layer (`Backend/ScriptFlow.API/Application`)

- `DTOs/`: `PrescriptionDto`, `MedicationDto`, `PatientDto`, `ProviderDto`, request DTOs
  (`CreatePrescriptionRequest`, `MedicationRequest`, `UpdatePrescriptionRequest`,
  `CreatePatientRequest`, `CreateProviderRequest`, `RegisterUserRequest`, `LoginRequest`), and
  `AuthResponse` (JWT + expiry).
- `Interfaces/`: `IPrescriptionRepository`, `IPatientRepository`, `IProviderRepository`,
  `IMedicineRepository`, `IPracticeRepository`, `IPracticeLocationRepository`,
  `IUserRepository`, `IPasswordHasher`, `IJwtTokenGenerator` (all implemented in Infrastructure).
- CQRS via **MediatR** + **FluentValidation** validation-pipeline behavior:
  - `Commands/`: `CreatePrescriptionCommand`, `UpdatePrescriptionCommand`,
    `SignPrescriptionCommand`, `RepeatPrescriptionCommand`, `CreatePatientCommand`,
    `CreateProviderCommand`, `RegisterUserCommand`, `LoginCommand`.
  - `Handlers/`: one `IRequestHandler<,>` per command/query — orchestrates repository calls +
    domain methods, publishes integration events via `IEventPublisher` after a successful
    state change (create/sign/repeat).
  - `Queries/`: `GetPrescriptionByIdQuery`, `ListPrescriptionsQuery`, `GetPatientByIdQuery`,
    `SearchPatientsQuery` (by name and/or NHI, backs the patient-lookup step of prescription
    creation), `GetProviderByIdQuery`.
  - `Validators/`: one `AbstractValidator<T>` per command, encoding the field rules from the
    "PreRequisites" section (required fields, NHI/HPI/SCID formats, medication list non-empty,
    quantity > 0, etc.). A `ValidationBehavior<TRequest,TResponse>` MediatR pipeline behavior
    runs these and throws a `ValidationException` → mapped to 400.
  - `Behaviors/`: `LoggingBehavior<TRequest,TResponse>` — a MediatR pipeline behavior that logs
    request name, correlation ID, duration, and outcome (success/failure) around every command
    and query, so the full application call trace is visible in structured logs and errors are
    easy to locate (per your logging requirement).

## Infrastructure layer (`Backend/ScriptFlow.API/Infrastructure`)

- `Persistence/`: `InMemoryPrescriptionRepository`, `InMemoryPatientRepository` (supports a
  simple case-insensitive contains-match search over name/NHI for `SearchPatientsQuery`),
  `InMemoryProviderRepository`, `InMemoryMedicineRepository` (seeded with a handful of sample
  medicines at startup), `InMemoryPracticeRepository`, `InMemoryPracticeLocationRepository`
  (seeded with a sample practice + practice location), `InMemoryUserRepository` — thread-safe
  (`ConcurrentDictionary`), registered as singletons. Documented limitation: data doesn't
  survive restarts; real EF Core + SQL Server schema is a follow-up pass once
  `DatabaseSpec.md` is filled in.
- `Auth/`: `JwtTokenGenerator` (`IJwtTokenGenerator`, using `Microsoft.IdentityModel.Tokens` /
  `System.IdentityModel.Tokens.Jwt`), `PasswordHasher` (`IPasswordHasher`, wrapping
  `Microsoft.AspNetCore.Identity.PasswordHasher<User>` — no full ASP.NET Identity/EF needed).
- `Messaging/`: thin adapter registering `Shared.Infrastructure`'s `RabbitMqEventPublisher` as
  `IEventPublisher` for this service (binds `RabbitMqOptions` from `appsettings.json`).

## Api layer (`Backend/ScriptFlow.API/APi/Controllers`)

- `AuthController`: `POST /api/auth/register`, `POST /api/auth/login` → returns JWT.
- `PatientsController`: `POST /api/patients`, `GET /api/patients/{id}`,
  `GET /api/patients/search?query=` (matches name or NHI, used to find a patient before
  creating a prescription against them).
- `ProvidersController`: `POST /api/providers`, `GET /api/providers/{id}`.
- `PrescriptionsController` (all `[Authorize]`):
  - `POST /api/prescriptions` → 201 Created (FR-001, FR-003 validates patient exists)
  - `PUT /api/prescriptions/{id}` → update (only while `Created`)
  - `POST /api/prescriptions/{id}/sign` → 200, transitions to `Signed`, publishes
    `PrescriptionSignedEvent` (FR-002)
  - `POST /api/prescriptions/{id}/repeat` → 201, publishes `PrescriptionRepeatedEvent`
  - `GET /api/prescriptions/{id}`, `GET /api/prescriptions?patientId=&status=` (FR-005/retrieve)
- `Middleware/ExceptionHandlingMiddleware`: maps `ValidationException`→400,
  `UnauthorizedAccessException`→401, `EntityNotFoundException`→404,
  `InvalidPrescriptionStateException`→409, everything else→500. Returns a consistent JSON
  problem-details body.
- `Program.cs` (composition root): Serilog bootstrap (from `Shared.Infrastructure`) with
  `UseSerilogRequestLogging`, correlation-ID middleware, MediatR + FluentValidation +
  `LoggingBehavior` pipeline registration, JWT bearer auth + authorization, RabbitMQ options
  binding + `IEventPublisher` registration, repository/service DI registrations, Swagger with
  JWT bearer security definition, exception middleware, controllers. Together these give an
  end-to-end call trace per request: correlation ID → HTTP log → per-command/query log →
  structured error log on failure.

## NuGet packages to add

- `ScriptFlow.API`: `MediatR`, `FluentValidation.DependencyInjectionExtensions`,
  `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core`
  (for `PasswordHasher<T>`), `Serilog.AspNetCore` (already pulled via Shared.Infrastructure but
  referenced directly for `UseSerilogRequestLogging`).
- `Shared.Infrastructure`: `RabbitMQ.Client`, `Serilog.AspNetCore`.

## Implementation order

1. `Shared.contract`: enums (`PrescriptionStatus`, `ProviderType`).
2. `Shared.Events`: `IntegrationEvent` base + the three event DTOs.
3. `Shared.Infrastructure`: correlation ID accessor/middleware, Serilog bootstrap extension,
   `IEventPublisher` + `RabbitMqEventPublisher` + options.
4. `ScriptFlow.API/Domain`: value objects, entities, exceptions, `Prescription` state machine.
5. `ScriptFlow.API/Application`: DTOs, repository/service interfaces, commands, validators,
   handlers, queries.
6. `ScriptFlow.API/Infrastructure`: in-memory repositories + seed data, JWT/password services,
   messaging adapter.
7. `ScriptFlow.API/APi`: controllers, exception middleware.
8. `Program.cs` + `appsettings.json` (add `Jwt` and `RabbitMq` config sections) composition.
9. Build (`dotnet build ScriptFlow.sln`) and fix any compile errors.

## Verification

- `dotnet build ScriptFlow.sln` succeeds with no errors.
- `dotnet run --project Backend/ScriptFlow.API` starts the API; Swagger UI loads at `/swagger`
  with JWT "Authorize" button.
- Exercise via Swagger or `ScriptFlow.API.http`: register → login → create patient → search for
  that patient → create provider (with a seeded practice location) → create prescription
  (with 1+ medications) → sign it → repeat it → get by id → list by patient. Confirm
  400/401/404/409 responses for invalid input, missing token, unknown patient, and
  double-signing respectively.
- Confirm structured logs show a correlation ID threaded through the HTTP request log and each
  command/query log line, so a single request's call trace can be followed end-to-end.
- If no local RabbitMQ broker is running, confirm the app still starts and prescription
  operations still succeed, with a logged warning for the failed publish (documented
  resilience behavior, not a bug).
