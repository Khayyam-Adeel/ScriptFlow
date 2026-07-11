# ScriptFlow Security Baseline & OWASP Top 10 Self-Assessment

This document covers the two security-related deliverables from the project brief:
a summary of the security baseline actually in place, and a short OWASP Top 10
(2021) self-assessment. Both are written against the real code, with file
references, not generic checklist claims — including the gaps.

## 1. Security baseline

**Authentication.** All backend API traffic (`ScriptFlow.API`, `Notification.Service`)
authenticates via JWT bearer tokens (HMAC-SHA256), issued by `ScriptFlow.API` on
register/login (`Infrastructure/Auth/JwtTokenGenerator.cs`) and validated identically
by both services. Passwords are hashed with ASP.NET Core Identity's PBKDF2
`PasswordHasher<T>` (`Infrastructure/Auth/PasswordHasher.cs`) — never stored or
logged in plaintext. Every data controller carries `[Authorize]`
(`APi/Controllers/*.cs`); only register/login are anonymous, by design.

**Authorization.** Currently authentication-only — any authenticated user can call
any endpoint. There is no role concept (`Domain/Entities/User.cs` has no `Role`
field) and no per-practice/ownership scoping on reads (e.g. one provider can fetch
another provider's prescriptions by id). This is a known, tracked gap — see A01
below — deliberately deferred to a separate pass, not silently omitted.

**Input validation.** Every mutating command has a FluentValidation validator
(`Application/Validators/*.cs`, 9 files covering all 8 commands, including a shared
`MedicationLineValidator` for embedded medication lines), registered globally as a
MediatR pipeline behavior, so no handler can run against invalid input.

**Parameterized data access.** All persistence goes through Dapper
`CommandDefinition` calls to named stored procedures with parameter objects / table-
valued parameters (`Infrastructure/Persistence/Sql*Repository.cs`). There is no
string-concatenated or inline SQL anywhere in the codebase.

**Secrets in configuration.** JWT signing key, RabbitMQ credentials, and the SQL
connection string all live in `appsettings*.json` / docker-compose environment
variables — none are hardcoded in source. The committed dev JWT key is an obvious
`CHANGE_ME_...` placeholder. RabbitMQ's `guest`/`guest` previously shipped as the
literal working value in the base `appsettings.json` of all three consuming
services; **fixed** — it now lives only in each service's `appsettings.Development.json`,
and `RabbitMqOptions.cs`'s C# property defaults were changed from `"guest"` to
`string.Empty`, so a non-Development environment that forgets to supply credentials
fails to connect (fail closed) instead of silently working against a shipped default.

## 2. OWASP Top 10 (2021) self-assessment

| # | Category | Status | Notes |
|---|---|---|---|
| A01 | Broken Access Control | **Gap** | Authentication is enforced everywhere, but there is no role-based authorization and no ownership/tenant scoping on reads — any authenticated user can access any other user's data by id. Tracked as a separate, deliberately deferred piece of work. |
| A02 | Cryptographic Failures | Addressed | Passwords: PBKDF2 via `PasswordHasher<T>`. JWTs: HMAC-SHA256, short-lived (60 min default), signing key sourced from config/env, never in source. HTTPS redirection enabled (`Program.cs`). Minor accepted risk: `TrustServerCertificate=True` on the SQL connection string weakens TLS validation — standard for local/dev SQL Server containers, should use a real cert in any hosted environment. |
| A03 | Injection | Addressed | 100% of data access is parameterized stored-procedure calls via Dapper (see baseline above); verified by grep across `Infrastructure/Persistence/` — no dynamic SQL exists. |
| A04 | Insecure Design | Partial | The prescription lifecycle is modeled as an explicit state machine with guard clauses (`Domain/Entities/Prescription.cs`), and validation runs at the API boundary before any handler executes. Gap: no rate limiting/throttling on `AuthController`'s login endpoint — a brute-force/credential-stuffing risk with no mitigation today. |
| A05 | Security Misconfiguration | Addressed (two issues fixed) | Swagger only enabled in `Development`; CORS restricted to an explicit origin allow-list (`Cors:AllowedOrigins`), not a wildcard. **Fixed**: `ExceptionHandlingMiddleware.cs` was forwarding the raw `exception.Message` to clients even for unhandled/500 errors; it now returns the generic title only for that branch, while still logging the full exception server-side. **Fixed**: RabbitMQ `guest`/`guest` was committed as a real working default in the base `appsettings.json` of all three services — moved to `appsettings.Development.json` only, with `RabbitMqOptions.cs`'s C# defaults blanked too, so production now fails closed instead of silently working. |
| A06 | Vulnerable and Outdated Components | Partial (backend fixed, frontend open) | Backend: `dotnet list package --vulnerable --include-transitive` flagged `System.Net.Http 4.3.0` / `System.Text.RegularExpressions 4.3.0` (High) pulled transitively into `ScriptFlow.API.Tests` via `xunit 2.5.3`; **fixed** by bumping `xunit`→2.9.3 / `xunit.runner.visualstudio`→3.1.5 (test-only dependency, never shipped, but now clean — re-verified with the same command, zero vulnerable packages remain). Frontend: `npm audit` reports 8 High-severity advisories across `@angular/core`/`common`/`compiler`/`forms`/`platform-browser` (including real XSS-bypass CVEs), all requiring a major-version jump to Angular 22 to clear. **Not fixed this pass** — a breaking upgrade needs its own deliberate, tested pass, not a drive-by dependency bump; documented here as an open, known risk rather than left undiscovered. |
| A07 | Identification and Authentication Failures | Addressed (two of three gaps fixed) | JWTs are short-lived and correctly validated (issuer/audience/lifetime/signing key). **Fixed**: added `POST /api/auth/logout`, which records the token's `jti` in a shared `IRevokedTokenStore` (`Shared.Infrastructure/Auth/`) and publishes `TokenRevokedEvent` over RabbitMQ so `Notification.Service` rejects the same token too — both services check revocation via `OnTokenValidated` in `Program.cs`, verified end-to-end (logout, then retry with the same token → 401). Known limitation: the store is in-memory per-process, so a restart forgets revocations (same trade-off already accepted for the idempotency store); acceptable given the 60-minute token lifetime. **Fixed**: `POST /api/auth/register` and `/login` are now rate-limited to 5 requests/minute per client IP (`AddPolicy("auth", ...)` in `Program.cs`, `[EnableRateLimiting("auth")]` on the two actions), verified end-to-end (6th request in a minute → 429). Accepted risk, not fixed: the token is still stored in browser `localStorage` (`token-storage.service.ts`) — a known XSS-token-theft surface with no httpOnly-cookie mitigation. Deliberately deferred: a cookie-based auth migration needs its own CSRF-protection design and Angular auth-flow rework, not a drive-by change alongside the other two fixes. |
| A08 | Software and Data Integrity Failures | Addressed | No unsafe/binary deserialization anywhere (all wire formats are `System.Text.Json`); the CI pipeline (build+test gate on every push/PR) reduces the risk of unreviewed code reaching `main`. Inter-service messages are trusted-network-only (RabbitMQ, no public exposure), so message-signing was not judged necessary at this scale. |
| A09 | Security Logging and Monitoring Failures | Partial | All four backend services use structured Serilog logging enriched with `Service` and `CorrelationId` (`Shared.Infrastructure/Logging/SerilogExtensions.cs`), so a single request/message can be traced end-to-end across services. Gap: the only sink is console output — no aggregated/centralized log store (Seq, ELK, App Insights) and no alerting on repeated auth failures. Fine for local/demo use, a real gap before any production deployment. |
| A10 | Server-Side Request Forgery (SSRF) | Not applicable | No endpoint accepts a user-supplied URL for the server to fetch. `Dispatch.Worker`'s call to the pharmacy gateway uses a fixed, operator-configured `PharmacyGateway:BaseUrl` — there is no code path where user input controls an outbound request target. |

## Summary

Four issues have been found and fixed across two passes: an information-disclosure bug
in the global exception handler and a transitive vulnerable-package chain in the test
project (A05/A06), plus a committed default RabbitMQ credential, missing login
throttling, and missing server-side token revocation (A05/A07) — the last two verified
end-to-end against a real database and message broker, not just reasoned about. Three
items remain open and are tracked as deliberate, separate follow-up work rather than
silently accepted: role-based authorization (A01), the Angular dependency upgrade
(A06), and JWT storage in `localStorage` plus production-grade log aggregation/alerting
(A07/A09).
