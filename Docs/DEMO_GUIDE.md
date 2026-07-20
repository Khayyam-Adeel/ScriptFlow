# ScriptFlow — Demo Guide & Speaker Notes (20–30 min)

Purpose of this file: so you can talk through the demo without vague answers. Every
number/claim below is taken directly from the repo's own docs (`README.md`,
`SystemReference.md`, `SECURITY.md`, `PERFORMANCE.md`, `SPEC/*.md`) as of 2026-07-15.
If asked something not covered here, it's safer to say "that's an open gap, here's
why" (see §7) than to guess.

---

## 0. One-sentence pitch

"ScriptFlow is an electronic-prescribing platform: a doctor creates and signs a
prescription, the system dispatches it to a simulated, deliberately unreliable
pharmacy gateway, and the doctor watches the outcome update live — built as four
cooperating .NET 8 services connected by RabbitMQ, plus an Angular 18 frontend."

Say this first. It's the sentence that orients everyone before you touch the keyboard.

---

## 1. Suggested running order (~25 min, adjust to your slot)

| # | Segment | Time | What you show |
|---|---|---|---|
| 1 | Pitch + architecture diagram | 2 min | §0, §2 |
| 2 | Live workflow: register → login → patient → provider → prescription → sign | 8 min | §3 |
| 3 | Watch Dispatch.Worker + PharmacyGateway do their thing, live status board updates via SignalR | 4 min | §3.6 |
| 4 | Rejection / repeat / error-handling workflow | 3 min | §3.7–3.8 |
| 5 | Security: JWT, roles, rate limiting, logout/revocation | 4 min | §4 |
| 6 | Performance chapter (1.05M rows, before/after) | 3 min | §5 |
| 7 | Honest gaps / what's next | 2 min | §7 |

If time is short, cut §6 to just the headline numbers and skip live queries.

---

## 2. Architecture — what to say while pointing at the diagram

```
ScriptFlow-UI (Angular)
      │  REST + SignalR
      ▼
ScriptFlow.API ──publishes PrescriptionSignedEvent──▶ RabbitMQ ──▶ Dispatch.Worker
      ▲                                                                 │
      │                                                                 ▼
      │  PrescriptionAcknowledged/RejectedEvent (via RabbitMQ)  PharmacyGateway.mock
      └──────────────────────────────────────────────────────── (slow, unreliable)
      │
      ▼  PrescriptionStatusChangedEvent (via RabbitMQ)
Notification.Service ──SignalR──▶ browser (live status board)
```

**Five backend projects, one frontend, all in one solution (`ScriptFlow.sln`):**

1. **`ScriptFlow.API`** — the core REST API. Owns prescriptions, patients, providers,
   practices, auth. Layered `Controller → Application → Domain → Infrastructure`
   (clean architecture), CQRS via **MediatR**, validation via **FluentValidation**,
   data access via **Dapper** calling stored procedures (no EF Core, no raw inline SQL).
2. **`Dispatch.Worker`** — a background `Worker Service`. Consumes
   `PrescriptionSignedEvent` off RabbitMQ, calls the pharmacy gateway, publishes the
   outcome back.
3. **`PharmacyGateway.mock`** — a fake third-party pharmacy. Random delay
   (100–2000ms), then a weighted roll: **~60% Ack, ~20% Reject, ~20% Drop** (severs
   the connection to simulate a network failure).
4. **`Notification.Service`** — SignalR hub. Relays status changes to every connected
   browser in real time, no polling.
5. **`Shared.contract` / `Shared.Events` / `Shared.Infrastructure`** — dependency-free
   enums/DTOs, RabbitMQ event contracts, and cross-cutting plumbing (correlation IDs,
   Serilog, RabbitMQ pub/sub, idempotency) shared by all four services so each new
   consumer is a few lines of DI registration, not new plumbing.
6. **`ScriptFlow-UI`** — Angular 18, standalone components (no NgModules). Talks to
   the API over REST and to `Notification.Service` over a SignalR WebSocket at
   `/hubs/prescriptions`.

**If asked "why RabbitMQ and not just direct HTTP calls between services?"** —
because the pharmacy gateway is deliberately unreliable; you need retries, a
dead-letter path for poison messages, and services that don't have to be up
simultaneously. A topic exchange (`scriptflow.events`) with one queue + DLQ per
consumer gives all of that without hand-rolling retry topology per service.

---

## 3. Live workflow script

Speak each step out loud as you click — don't silently demo.

### 3.1 Register / Login
- `POST /api/auth/register` (anonymous) — creates a user. **Always creates a
  `Prescriber` role, never client-supplied** — this is a deliberate security control,
  say it out loud (§4.2).
- `POST /api/auth/login` — returns a JWT (HMAC-SHA256, 60-min default expiry).
- Frontend: login screen → token stored, attached via an Angular HTTP interceptor to
  every subsequent request.

### 3.2 Create a Patient
- Fields: **First name, Last name, Address, NHI**.
- **NHI** = NZ National Health Index number, format `[A-Z]{3}[0-9]{4}` (e.g.
  `ABC1234`) — validated as a domain value object (`Nhi.cs`), throws `DomainException`
  if malformed, not just a UI regex.

### 3.3 Create a Provider (needs a seeded Practice Location first)
- Fields: **First name, Last name, Type** (Doctor/Nurse/Student), **NZMC number**.
- Practice location carries an **HPI number** (Health Point Identifier):
  `[A-Z]{3}[0-9]{2}` + a letter extension, displayed as `FZZ99-B`. Also a validated
  value object.
- **`POST /api/providers` is Admin-only** — mention this here, tie back to §4.2 RBAC.

### 3.4 Create a Prescription
- `POST /api/prescriptions` → **201**.
- One or more medication lines; each has **medicine name, SCTID (SNOMED CT code),
  form, duration, take value, frequency, quantity, direction**.
- Prescription itself gets a **SCID** (its own identifier, distinct from a medicine's
  SNOMED code): `9` + 5 alphanumeric chars (EPS entity number) + 5 more alphanumeric
  chars, e.g. `9J0BGVA1B2C`.
- Status starts at **Created**.
- Every command is validated by a dedicated FluentValidation validator running as a
  MediatR pipeline behavior — **no handler ever executes against invalid input**, the
  validation happens before the handler is even reached.

### 3.5 Sign the Prescription
- `POST /api/prescriptions/{id}/sign` → **200**, publishes `PrescriptionSignedEvent`.
- State machine transition: **Created → Signed**. Say out loud: *"ScriptFlow.API only
  ever drives Created → Signed itself — everything after that (Dispatched,
  Acknowledged, Rejected) is driven by events coming back from Dispatch.Worker."*
- Full lifecycle: **Created → Signed → Dispatched → Acknowledged | Rejected**, with
  **Expired** reachable from any non-terminal state. Enforced by guard clauses in
  `Prescription.cs` — an illegal transition throws `InvalidPrescriptionStateException`
  → mapped to **409 Conflict** by the exception middleware.

### 3.6 Watch the dispatch happen (the centerpiece — narrate this in real time)
- Switch to `Dispatch.Worker`'s console/logs.
- Say: *"The worker just picked `PrescriptionSignedEvent` off its RabbitMQ queue. It
  checks an idempotency store first — if this event was already processed, it's
  skipped, so a redelivered message can never double-dispatch a prescription."*
- It calls `PharmacyGateway.mock` via `POST /api/pharmacy/dispatch`. The gateway
  sleeps a random 100–2000ms, then rolls: Ack (~60%) / Reject (~20%) / Drop (~20%,
  severs the connection with `HttpContext.Abort()`).
- **If Drop happens live** (decent odds within a couple of tries): *"That was a
  dropped connection — Polly just retried it: 3 attempts, exponential backoff 2s → 4s
  → 8s. Only a Reject is treated as final — a rejection like 'out of stock' is a real
  business answer, not a transient fault, so it's never retried."*
- Switch to the Angular dashboard: the status board updates **without a page refresh**
  — that's `Notification.Service` relaying `PrescriptionStatusChangedEvent` over the
  SignalR hub at `/hubs/prescriptions` to every connected browser.

### 3.7 Rejection handling workflow
- Show a prescription that lands on **Rejected**.
- This is a legitimate terminal state, not an error — the UI should surface it as a
  distinct status (status-badge component), not a generic failure toast.

### 3.8 Repeat a prescription
- `POST /api/prescriptions/{id}/repeat` → **201**, publishes `PrescriptionRepeatedEvent`.
- Only legal from `Signed`, `Dispatched`, or `Acknowledged` — copies the medication
  list into a brand-new `Prescription` (status back to `Created`) with
  `RepeatOfPrescriptionId` pointing at the source.

### 3.9 Error handling (worth 60 seconds to show deliberately)
- Show one deliberate bad request each, so it's not just claimed:
  - Bad/missing field → **400** (FluentValidation)
  - No/expired token → **401**
  - Unknown patient ID → **404**
  - Double-signing an already-signed prescription → **409**
- All mapped centrally in `ExceptionHandlingMiddleware` to a consistent
  problem-details JSON body — not ad hoc per controller.

---

## 4. Security — this is likely to get the most questions

### 4.1 Authentication
- JWT bearer tokens, **HMAC-SHA256**, issued on register/login
  (`JwtTokenGenerator.cs`).
- Passwords hashed with ASP.NET Core Identity's **PBKDF2** `PasswordHasher<T>` — never
  stored or logged in plaintext.
- Same signing key/issuer/audience validated identically by `ScriptFlow.API` and
  `Notification.Service` (config must match between the two — mention this is a real
  footgun if someone forgets to sync it).
- `Notification.Service`'s SignalR hub can't read an `Authorization` header on a
  WebSocket handshake, so the token is passed as an `access_token` query-string
  param and pulled back out server-side for validation.

### 4.2 Authorization — role-based
- `User.Role` = `Prescriber` or `Admin`, flows into the JWT as a `ClaimTypes.Role`
  claim, enforced natively by `[Authorize(Roles = ...)]`.
- **Admin-only:** `POST /api/providers` (register a new provider) and
  `POST /api/auth/register-admin` (create another Admin). Everything else — patient
  and prescription management — is open to any authenticated user, since that's
  normal day-to-day clinical work for either role.
- **Self-escalation is closed:** `POST /api/auth/register` always hardcodes
  `Prescriber` server-side — the role is never client-supplied, so an unauthenticated
  caller can never mint an Admin account.
- The only way to create a new Admin is `register-admin`, which itself requires an
  existing Admin's bearer token, and — deliberately — does **not** return a token for
  the account it creates, so the caller stays logged in as themselves.
- The very first Admin in a deployment has no bootstrap endpoint by design (nothing
  can call an Admin-only route before an Admin exists) — created via a one-off SQL
  `UPDATE` on a self-registered user. If asked "isn't that a hole?" — no, it's the
  standard chicken-and-egg answer for the first privileged account in any system with
  no seed data.
- **Verified, not just claimed:** a fresh Prescriber gets 403 on both admin routes;
  the same user promoted to Admin gets 201 on both.
- **Known, accepted gap (say this proactively, don't wait to be asked):** no
  per-practice/ownership scoping on reads yet — one provider can fetch another
  provider's prescriptions by ID if they know it. Tracked as further work.

### 4.3 Other hardening (mention if time allows / if asked "what about OWASP?")
- **Injection:** 100% of data access is parameterized stored-procedure calls via
  Dapper — no string-concatenated or inline SQL anywhere, verified by grep.
- **Rate limiting:** `/api/auth/register` and `/api/auth/login` are limited to
  **5 requests/minute per client IP**; the 6th request in a minute gets **429**,
  verified end-to-end. Mitigates brute-force/credential stuffing.
- **Logout / token revocation:** `POST /api/auth/logout` records the token's `jti` in
  a shared revocation store and publishes `TokenRevokedEvent`, so both `ScriptFlow.API`
  and `Notification.Service` reject the same token afterward — verified: logout, then
  retry the same token → 401. Known limitation: the store is in-memory per process, so
  a restart forgets revocations (acceptable given the 60-min token lifetime).
- **Secrets:** JWT signing key, RabbitMQ credentials, SQL connection string all live in
  config/env, never hardcoded. Dev JWT key is an obvious `CHANGE_ME_...` placeholder.
  RabbitMQ's `guest`/`guest` was previously a committed working default — **fixed**:
  it now only lives in `appsettings.Development.json`, and the C# defaults were
  blanked so production fails closed instead of silently working with weak creds.
- **Information disclosure fix:** the global exception handler used to leak raw
  `exception.Message` to clients on 500s — **fixed** to return a generic message while
  still logging the full exception server-side.
- **Honest open gaps, own them if asked, don't dodge:**
  - JWT is stored in browser `localStorage` — an XSS-token-theft surface; no
    httpOnly-cookie migration yet (that needs its own CSRF design, deliberately
    deferred, not overlooked).
  - Frontend has 8 High-severity `npm audit` findings on Angular packages, fixable
    only by a major-version jump to Angular 22 — documented, not silently ignored.
  - **Fixed since this guide was first written:** all four services now also write
    to a rolling daily file sink (`Serilog.Sinks.File`) under `Logs/<Service>-.log`
    in each service's working directory (14-day retention), in addition to console —
    so a run's logs survive after the terminal closes. Still no centralized
    aggregation (Seq/ELK/App Insights) or alerting on repeated auth failures — that
    remains an open gap before any production deployment.
  - `TrustServerCertificate=True` on the SQL connection string — standard for
    local/dev SQL Server, would need a real cert in a hosted environment.

**If asked "did you do a full OWASP review?"** — yes, `SECURITY.md` has a full OWASP
Top 10 (2021) table, A01–A10, each with file references, not generic checklist
claims. Five issues were found and fixed across three passes (info disclosure,
vulnerable test-only package, committed default credential, missing login throttling,
missing role-based auth). Two remain open and documented (Angular upgrade,
localStorage token storage + log aggregation).

---

## 5. Performance chapter

**Headline:** seeded `Prescription.tblPrescriptions` to **1,050,000 rows** (plus a
matching medication row each) in a real local SQL Server instance — not estimated,
every number below came from actually running `SET STATISTICS IO/TIME/XML ON`.

**Final status breakdown after seeding:** Created 52,435 / Signed 83,676 / Dispatched
42,093 / Acknowledged 662,753 / Rejected 209,043.

| Query | Logical reads before → after | Result |
|---|---|---|
| 1. Dispensing volumes (by practice location/month) | 32,343 → 3,785 | **−88%**, new covering index |
| 2a. Rejection rate by practice location | 32,343 → 5,102 | **−84%**, new narrow composite index |
| 2b. Rejection rate by provider | 32,343 → 5,102 | **−84%**, same pattern |
| 3. Repeat-due list (signed >90 days, no repeat yet) | 33,006 → 33,006 | **No change** — reported honestly |

**Say this about query 3 if asked "why didn't the index help?"** — the filtered index
assumed the 90-day cutoff would be selective. It wasn't: 55.3% of the seeded rows
(580,217 of 1,050,000) matched the outer filter, so a full clustered scan was
genuinely cheaper than a nonclustered seek plus bookmark lookups — SQL Server's
optimizer correctly ignored the new index. The lesson: **index design has to be
checked against actual predicate selectivity, not assumed from the query's English
description.** This is a good answer to lead with if asked "what did you learn" — it
shows you measured rather than assumed, and reported a negative result instead of
hiding it.

**The seed also exposed a real bug, worth telling as a story:** the dashboard called
an unfiltered `GET /api/prescriptions` and counted statuses client-side. Before the
seed this was instant; against 1.05M rows the request never completed even after 15
seconds. Fixed two ways: (1) `usp_Prescription_List` capped at 200 most recent rows,
(2) a dedicated `usp_Prescription_StatusCounts` endpoint backs the dashboard's status
counts instead — because capping the list alone would make the dashboard "fast but
wrong" (only counting the 200 most recent rows out of 1.05M). Verified live:
status-counts returns real totals (Acknowledged 662,759, Rejected 209,045) in ~3s.

**One more honest correction, good "what went wrong" answer if asked:** adding the
filtered index in query 3 required `QUOTED_IDENTIFIER ON` for every session writing
to that table — but `usp_Prescription_Create`/`usp_Prescription_Update` were compiled
with it OFF, so from the moment that index was added, every create/sign call started
failing with 500. Caught by the integration test actually exercising create→sign
end-to-end, fixed by recompiling both procedures with `QUOTED_IDENTIFIER ON`.

---

## 6. Testing & CI (say this proactively — it shows rigor)

- Test project: `Backend/ScriptFlow.API.Tests` (xUnit + coverlet), covering the
  Domain layer: `Prescription`'s full state machine (every legal/illegal transition),
  all entity guard clauses, and the three value objects (`Scid`, `Nhi`, `HpiNumber`).
- Plus an integration test (`PrimaryWorkflowIntegrationTests`) exercising the real
  ASP.NET Core pipeline end-to-end against real SQL Server + RabbitMQ containers.
- **CI** (`.github/workflows/ci.yml`): runs on every push/PR to `main`. Backend job
  spins up real SQL Server + RabbitMQ service containers (not fakes/mocks), bootstraps
  the schema, then `dotnet test` with coverage collection uploaded as an artifact.
  Frontend job runs `npm run build` + Angular unit tests headless.

---

## 7. If asked "what's not done yet" — answer this directly, don't dodge

Own these; they read as engineering maturity, not a failure, when stated plainly with
the reason:

1. **No per-practice/ownership scoping on reads** — a provider can currently fetch
   another provider's prescriptions by ID. Tracked, not silently omitted.
2. **Angular dependency upgrade** — 8 High `npm audit` findings need a major version
   jump (Angular 22) to clear; deliberately deferred as its own breaking-change pass.
3. **JWT in `localStorage`** — XSS-token-theft surface; httpOnly-cookie migration
   needs its own CSRF design, deliberately deferred.
4. **In-memory idempotency/revocation stores** — reset on process restart. Acceptable
   trade-off given short token lifetimes, but not durable across restarts. A durable
   `ProcessedMessages` SQL table is designed in `DatabaseSpec.md` as the future
   replacement.
5. **Docker Compose orchestration is unverified** — every service has a Dockerfile and
   `docker-compose.yml` wires them together, but it's never actually been run end to
   end. Docker Desktop + WSL2 are now installed on this machine, but the daemon isn't
   yet reliably responsive here, and `docker compose up` hasn't been exercised. What
   *is* verified: the SQL schema/seed scripts it would run were executed directly
   against a real SQL Server instance and work. If asked "is it containerized?" — say
   "shovel-ready, not proven" and explain why (ADR-004 in the README covers the
   full reasoning).
6. **Console-only logging** — no centralized log aggregation or alerting yet; fine for
   demo/local, a real gap before production.

---

## 7a. Tracing an error back to the database — step by step

This is a strong thing to demo live if asked "how would you debug a production
issue?" — it shows the correlation ID + structured logging + DB error log actually
connect, not just exist independently.

**The three places an error can leave a trace, and how they connect:**

1. **The HTTP response body itself** (what the caller/UI sees) — a problem-details
   JSON object from `ExceptionHandlingMiddleware.cs`:
   ```json
   { "title": "...", "status": 500, "detail": "...", "traceId": "0HN7...:00000001" }
   ```
   Note: `traceId` here is **ASP.NET Core's own `HttpContext.TraceIdentifier`**, not
   the same value as the correlation ID below — this is a real, worth-knowing wrinkle
   (see the callout at the end).

2. **The `X-Correlation-Id` response header** — set by `CorrelationIdMiddleware`
   (`Shared.Infrastructure/Correlation/CorrelationIdMiddleware.cs`) on *every* request,
   success or failure. Either echoes a client-supplied `X-Correlation-Id` request
   header, or mints a new GUID if none was sent. **This is the ID that actually
   threads through the logs and, for dispatch-pipeline events, through RabbitMQ
   messages too.**

3. **The per-service log file** — `Logs/<ServiceName>-YYYYMMDD.log` in each service's
   working directory (e.g. `Backend/PharmacyGateway.mock/Logs/PharmacyGateway.mock-
   20260715.log` when run via `dotnet run`), rolling daily, 14 days retained. Same
   content and format as the console output — added specifically so logs survive
   after the terminal is closed. `*.log` is gitignored, so these never get committed.

4. **The `dbo.TblErrorLog` table** — only populated when a *stored procedure* itself
   throws (a SQL-level failure: constraint violation, deadlock, bad data), via the
   `BEGIN CATCH` block every stored procedure has (see `usp_Prescription_Create.sql`
   for the pattern). Columns: `ID`, `Error` (truncated message, 200 chars),
   `StoreProcedure` (which proc), `ErrorStack` (full `ERROR_NUMBER/SEVERITY/STATE/
   LINE/PROCEDURE/MESSAGE`), `InsertedAt`. **Important: this table does NOT store the
   correlation ID** — it's SQL-side, with no concept of the request that called it.

### Step-by-step trace

1. **Start from what the user/tester reports** — either a screenshot of the error
   toast (has no correlation ID visible in the UI today) or, better, the failed
   HTTP response captured from browser DevTools → Network tab. Grab the
   **`X-Correlation-Id` response header** from that request (not the `traceId` field
   in the JSON body — different ID, see the callout below).

2. **Grep the console logs of every backend service for that correlation ID.**
   Every log line across all four services is tagged with it — the Serilog output
   template (`Shared.Infrastructure/Logging/SerilogExtensions.cs`) is:
   ```
   [HH:mm:ss LVL] {Service} ({CorrelationId}) {SourceContext}: {Message}
   ```
   So `grep "a1b2c3d4-..." api.log dispatch-worker.log notification.log` (or the
   equivalent in your terminal/log viewer) shows you **every step that request or
   event touched, across every service it flowed through** — because
   `CorrelationIdMiddleware` pushes it into Serilog's `LogContext` for the whole
   request, and `LoggingBehavior<TRequest,TResponse>` (the MediatR pipeline behavior)
   logs entry/exit/duration/failure for every single command or query using that
   same ID.

3. **Identify which service and which command/query failed** from the log line
   itself — `LoggingBehavior` logs `"Failed handling {RequestName} [{CorrelationId}]
   after {ElapsedMilliseconds}ms"` with the full exception attached, so you get the
   .NET stack trace right there without touching the database yet.

4. **If the failure is a genuine unhandled exception in application code** (not a SQL
   error), you're done — the stack trace in step 3 *is* the root cause.
   `ExceptionHandlingMiddleware` additionally logs it once more at the top (`"Unhandled
   exception"`) for any 500 that reaches it, so it's guaranteed to appear even for
   exceptions the LoggingBehavior didn't wrap (e.g. thrown outside a MediatR handler).

5. **If the failure originated inside a stored procedure** (SQL error — constraint
   violation, deadlock, truncation, etc.), the .NET-side log line tells you *that* a
   SQL call failed and roughly *when*, but not the exact SQL-level detail. Go to
   `dbo.TblErrorLog` and query by time window and procedure name, since there's no
   correlation ID to join on directly:
   ```sql
   SELECT TOP 20 *
   FROM dbo.TblErrorLog
   WHERE InsertedAt BETWEEN '<timestamp from log line minus a few seconds>'
                         AND '<timestamp from log line plus a few seconds>'
   ORDER BY InsertedAt DESC;
   ```
   Narrow further with `StoreProcedure = 'Prescription.usp_Prescription_Create'` (or
   whichever proc the app-layer log line named) if the time window returns more than
   one candidate row. The `ErrorStack` column has the full `ERROR_NUMBER/SEVERITY/
   STATE/LINE/PROCEDURE/MESSAGE` — that's the actual SQL Server-level root cause
   (e.g. a specific constraint name, a deadlock victim, a truncated value).

6. **For an error inside the async dispatch pipeline** (Dispatch.Worker →
   PharmacyGateway.mock → back through RabbitMQ), the same correlation ID from step 1
   carries through — `IntegrationEvent`'s base type (`Shared.Events`) carries a
   `CorrelationId` field, so `PrescriptionSignedEvent`, `PrescriptionAcknowledgedEvent`,
   etc. all propagate it. Grepping that one ID across `dispatch-worker` and
   `notification-service` logs shows the entire async hop, not just the initial HTTP
   request.

### ⚠️ The wrinkle worth knowing before you're asked about it

The `traceId` field returned in the **JSON error body** is `HttpContext.TraceIdentifier`
(ASP.NET Core's built-in per-request ID) — a **different value** from the
`X-Correlation-Id` **header** that actually appears in the Serilog logs. If someone
tries to grep the logs using the `traceId` from the error JSON, they won't find
anything. The correct value to grep with is the `X-Correlation-Id` **response
header**, not the body's `traceId` field. This is a real, honest gap — worth naming
proactively if asked "how do I trace an error", since it's the single most likely
way someone tracing a real error would get stuck.

**One-sentence summary if asked to explain this live:** *"Every request gets a
correlation ID that's echoed in the `X-Correlation-Id` response header and stamped on
every log line it touches across all four services and every RabbitMQ event it
produces — so I grep the logs for that ID to see the whole request's journey. If the
root cause is a SQL-level failure, the stored procedure itself additionally writes to
`dbo.TblErrorLog` with the full SQL error detail, which I look up by timestamp and
procedure name since that table doesn't carry the correlation ID."*

---

## 8. Rapid-fire fact sheet (memorize these numbers)

- **4 backend services** + 3 shared libraries + 1 Angular frontend, all in
  `ScriptFlow.sln`, targeting **.NET 8**.
- Pharmacy gateway odds: **60% Ack / 20% Reject / 20% Drop**, delay **100–2000ms**.
- Polly retry: **3 attempts, 2s/4s/8s backoff**, only on transient (Drop), never on
  Reject.
- Prescription state machine: **Created → Signed → Dispatched → Acknowledged |
  Rejected**, plus **Expired** from any non-terminal state.
- SCID format: `9` + 5 alphanumeric (EPS entity no) + 5 alphanumeric.
- NHI format: `[A-Z]{3}[0-9]{4}`.
- HPI format: `[A-Z]{3}[0-9]{2}` + extension letter, e.g. `FZZ99-B`.
- JWT: HMAC-SHA256, **60-min** default expiry.
- Auth rate limit: **5 requests/min per IP** on register+login, 429 on the 6th.
- Performance seed: **1,050,000 prescriptions**, logical reads cut **84–88%** on two
  of three reporting queries via targeted indexes; third query's index added no
  benefit (55.3% predicate selectivity, reported honestly).
- Roles: **Prescriber** (default, self-registerable) / **Admin** (gates
  `POST /api/providers` and `POST /api/auth/register-admin` only).

---

## 9. Anticipated Q&A

**Q: Why RabbitMQ instead of just calling services directly?**
A: The pharmacy gateway is intentionally unreliable — you need retry/backoff and a
dead-letter path for messages that will never succeed, and services need to be able
to be briefly down without losing work. A shared topic exchange with per-consumer
queues + DLQs gives idempotent, resilient messaging without hand-rolling it three
times.

**Q: How do you stop a duplicate message from double-processing?**
A: Idempotency store (`IProcessedMessageStore`) — before acting on an event, the
handler checks if it's already been processed and skips if so. Currently in-memory
(documented limitation, resets on restart); a durable SQL-backed version is designed
but not yet built.

**Q: What happens to a message that keeps failing?**
A: After Polly's 3 retries are exhausted, the consumer Nacks with `requeue:false`,
and RabbitMQ automatically routes it to that consumer's dead-letter queue — inspectable
in the RabbitMQ management UI, not silently lost or retried forever.

**Q: How is a prescriber prevented from becoming an Admin?**
A: The role is never client-supplied on self-registration — it's hardcoded to
Prescriber server-side. The only path to Admin is an existing Admin creating one via
`register-admin`, or a one-off SQL update for the very first Admin in a fresh
deployment.

**Q: Is this actually deployed anywhere?**
A: Yes — there's an AWS EC2 deployment path documented in `Docs/AWS-DEPLOYMENT.md`
with a prod docker-compose override and an nginx proxy for the API/SignalR traffic
(if you've run this, describe what you actually saw; if not, say "prepared and
documented, ask me about docker-compose.yml locally instead" rather than claim it's
live if you haven't verified it yourself before the demo).

**Q: What's the response-time requirement and do you meet it?**
A: Spec requires <500ms. Not something formally load-tested end-to-end in this repo's
docs — if pushed on this, say so rather than inventing a number. The performance
chapter's measured numbers are query-level (logical reads / CPU time), not full
HTTP round-trip latency under load.

---

## 10. Before you present — a short checklist

- [ ] Confirm SQL Server + RabbitMQ are running locally and reachable.
- [ ] Run all four backend services + the Angular dev server, confirm they start clean.
- [ ] Do one full dry-run of §3 end to end so you've actually seen an Ack, and ideally
      a Drop (to show the retry) and a Reject, before presenting live — odds are good
      within 3–4 signed prescriptions given the 60/20/20 split.
- [ ] Have Swagger open in one tab (shows JWT "Authorize" button, all endpoints) as a
      backup if a live UI click fails.
- [ ] Know where `SECURITY.md` and `PERFORMANCE.md` are so you can flip to them if a
      question goes deeper than this guide.
