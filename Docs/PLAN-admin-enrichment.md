# Enrich the Admin section

**Status: Implemented.** Verified in code — `PracticesController`/`PracticeLocationsController`
Create endpoints, the three `usp_Reporting_*` procedures, and the frontend's
`features/admin/admin-overview/` + `features/admin/admin-practices/` (alongside the
pre-existing `admin-dlq/` and `register-admin-user/`) all exist and are wired up.

## Context

Today "Admin" is a single nav link ("Admins") to one form: register a new admin user
(`features/admin/register-admin-user`). The user wants this area to become a real admin
console: practices/locations data, the ability to add new locations, and prescription
graphs/charts by practice and location.

Investigated what already exists to reuse rather than rebuild:
- **Practice & PracticeLocation are already half-wired**: `Domain/Entities/Practice.cs` and
  `PracticeLocation.cs`, their repositories (`SqlPracticeRepository`, `SqlPracticeLocationRepository`
  — both already have working `AddAsync`), and their stored procs
  (`usp_Practice_Create.sql`, `usp_PracticeLocation_Create.sql`) all already exist and work —
  they're just never called, because there's no `CreatePracticeCommand`/
  `CreatePracticeLocationCommand`/handler/controller action wiring them up yet. This is the
  same shape as the recent Patient/Provider field work, just for the *command* side instead
  of new fields — cheap to finish.
- **The reporting queries already exist too**, as read-only reference SQL in
  `Infrastructure/Database/Performance/03_ReportingQueries.sql` (written for the assignment's
  "Performance chapter" but never turned into an endpoint): dispensing volume by location,
  rejection rate by location, rejection rate by provider, and a repeat-due worklist. The
  indexes they need already exist too (`IX_Prescriptions_PracticeLocationId_Status`,
  `IX_Prescriptions_ProviderId_Status` from `Performance/04_Indexes.sql`) - turning these into
  real charts is exactly the kind of thing "enrich this screen with data and charts" is asking
  for, and it's real, already-designed analysis rather than fabricated content.
- **Dashboard's existing chart style** (`features/dashboard/dashboard.component.ts/html/css`)
  is hand-rolled CSS/SVG bars driven by a `maxCount` computed signal - no chart library. I'll
  match that convention for the new charts (checked against the `dataviz` skill: a ranked
  bar chart of one measure per named category is a single-hue magnitude encoding, not a
  categorical palette, so no multi-hue palette validation is needed - `--color-primary` for
  normal bars, with `--color-danger`/`--color-warning` thresholds + a direct label for the
  rejection-rate charts so color is never the only signal).
- **Admin authorization pattern**: `ProvidersController.Create` is `[Authorize(Roles =
  nameof(UserRole.Admin))]` on top of the controller's class-level `[Authorize]` - the exact
  pattern to mirror for the two new Create endpoints.

## Backend

### Practice & PracticeLocation create (Admin-only)
- `Application/Queries/GetPracticeByIdQuery.cs` + handler, `GetPracticeLocationByIdQuery.cs` +
  handler (so `CreatedAtAction(nameof(GetById), ...)` has somewhere to point, matching every
  other controller's convention).
- `Application/Commands/CreatePracticeCommand.cs` (Name) + validator + handler.
- `Application/Commands/CreatePracticeLocationCommand.cs` (PracticeId, Name, HpiNo,
  HpiExtension, Address, Phone) + validator (reuses the existing `HpiNumber` value object for
  format validation) + handler.
- `PracticesController`: add `[Authorize(Roles = nameof(UserRole.Admin))] POST` + `GET {id}`.
- `PracticeLocationsController`: same two additions.
- New DB migration for the local dev environment: none needed (no schema change, only new
  command wiring against tables/procs that already exist).

### Reporting endpoints (any authenticated user, like the existing dashboard stats)
- 3 new stored procs under `StoredProcedures/Prescription/`, adapted from
  `Performance/03_ReportingQueries.sql`:
  - `usp_Reporting_VolumeByLocation` - `COUNT(*)` per practice location (all statuses),
    `TOP (20)` by volume desc.
  - `usp_Reporting_RejectionRateByLocation` - Query 2a as-is (rejection % of finalized
    prescriptions per location).
  - `usp_Reporting_RejectionRateByProvider` - Query 2b as-is, `TOP (10)` by rate desc.
- `Application/DTOs`: `LocationVolumeDto(string LocationName, int Count)`,
  `RejectionRateDto(string Name, int RejectedCount, int FinalizedCount, decimal RejectionRatePct)`
  (shared shape for both location and provider rejection rates).
- 3 new methods on `IPrescriptionRepository`/`SqlPrescriptionRepository`
  (`GetVolumeByLocationAsync`, `GetRejectionRateByLocationAsync`,
  `GetRejectionRateByProviderAsync`), matching the existing `GetStatusCountsAsync`/
  `GetDailyVolumeAsync` pattern exactly.
- 3 new Queries+Handlers, exposed as `GET /api/prescriptions/reporting/volume-by-location`,
  `/rejection-rate-by-location`, `/rejection-rate-by-provider` on the existing
  `PrescriptionsController`.

## Frontend

### Models & services
- `practice.model.ts` gets `CreatePracticeRequest`; `practice.service.ts` gets `create()` +
  `getById()`.
- `practice-location.model.ts` gets `CreatePracticeLocationRequest`;
  `practice-location.service.ts` gets `create()` + `getById()`, and clears its cached
  `shareReplay` list (`cachedList$ = null`) after a successful create so the new location
  shows up immediately everywhere that list is used (provider form, prescription form, etc.).
- `prescription.model.ts` gets `LocationVolume`/`RejectionRate` interfaces;
  `prescription.service.ts` gets the 3 matching GET methods.

### New Admin IA (replacing the single "Admins" link with a small sub-nav)
`app-shell.component.html`'s `isAdmin()` block becomes three links instead of one:
- **`/admin` (Overview, new default)** - `features/admin/admin-overview/admin-overview.component`:
  - KPI tiles: Practices, Locations, Providers, Total prescriptions, Pending (reusing existing
    `GET /api/practices`, `/api/practice-locations`, `/api/providers` list lengths +
    `getStatusCounts()` already used by the dashboard - no new backend needed for these).
  - Three bar charts (hand-rolled CSS/SVG, matching `dashboard.component`'s style): volume by
    location, rejection rate by location, rejection rate by provider - each bar gets a native
    SVG `<title>` for a hover tooltip (cheap, meets the dataviz skill's "hover layer by
    default" baseline without a custom tooltip component).
- **`/admin/practices` (new)** - `features/admin/admin-practices/admin-practices.component`:
  practices listed with their locations nested underneath (reuses `GET /api/practice-locations`
  grouped client-side by `practiceId`), "+ New practice" and "+ New location" buttons opening
  the `app-modal` (already built for the prescription print feature) with a small reactive
  form each, posting to the new Create endpoints.
- **`/admin/register-admin` (unchanged)** - existing `RegisterAdminUserComponent`, just now one
  tab among three instead of the only page.
- `app.routes.ts`: add the two new routes (both `canActivate: [adminGuard]`, matching
  `providers/new`'s existing pattern).

## Verification
- `dotnet build` / `dotnet test` (new unit tests for the two new command validators, following
  `CreateProviderCommandValidator`'s existing test style).
- Redeploy the 3 new stored procs + the Practice/PracticeLocation Create procs (already
  written, just never deployed - not new SQL) to the local dev DB, same `ALTER PROCEDURE`
  approach used earlier this session (SQL Server 2014 doesn't support `CREATE OR ALTER`).
- `npm run build`.
- Browser walkthrough (reusing the Playwright verification approach from the print-preview
  feature): log in as an Admin user, open `/admin`, confirm the KPI tiles and three charts
  render with real numbers; go to `/admin/practices`, create a new practice location, confirm
  it immediately appears in the provider-creation practice-location picker.
