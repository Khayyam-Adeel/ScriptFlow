# Add fields to Patient & Provider registration

## Context

The Patient registration form only collects First name/Last name/Address/NHI, and
Provider registration only collects First name/Last name/Type/NZMC/Practice location.
The user wants both forms to look like real healthcare registration forms by adding:

- **Patient**: Date of birth, Gender, Phone number, Email
- **Provider**: Email, Phone number, Qualification

These must be fully persisted (DB → API → UI), not just cosmetic fields, per user
confirmation. ScriptFlow is a clean layered app (Domain Entity → Command → FluentValidation
Validator → MediatR Handler → Dapper/stored-proc Repository → DTO → Mapping → Controller),
mirrored on the Angular side (model → service → reactive form). Adding a field means
touching one file at each layer for each entity - verified by reading the full existing
chain for both Patient and Provider (see file list below). The DB has never been deployed
anywhere real yet (compose stack never ran end-to-end), so schema/proc files can be edited
directly - no ALTER-based migration needed.

## Design decisions

- **Gender**: new `Gender` enum (`Male=0, Female=1, Other=2`) in `Shared.contract.Enums`,
  mirroring the existing `ProviderType` enum pattern exactly (same project, same style).
  Frontend gets a matching `shared/models/gender.ts` mirroring `provider-type.ts`
  (`export type Gender = 'Male' | 'Female' | 'Other'; export const GENDERS = [...]`).
- **DateOfBirth**: `DateOnly` in C# / `DATE` in SQL. Domain validation: required, not in
  the future (matches the existing style of throwing `DomainException` in the entity
  constructor, e.g. `Patient.cs`'s existing null checks).
- **Email**: plain `string`, validated with FluentValidation's `.EmailAddress()`. Not a
  value object (existing precedent: `Address` is a plain validated string; only `Nhi` and
  `HpiNumber` warranted value objects because of their strict fixed-format patterns).
- **PhoneNumber**: plain `string`, FluentValidation `.Matches(@"^[0-9+\-\s()]{7,20}$")`.
- **Qualification** (Provider): plain free-text `string`, required, no format validation.
- All new fields are **required** (matches the "make it look like a real form" intent and
  the existing all-required style of both forms).

## Files to change

### Database
- `Backend/ScriptFlow.API/Infrastructure/Database/Schema/00_CreateSchema.sql`
  - `tblPatients`: add `DateOfBirth DATE NOT NULL`, `Gender TINYINT NOT NULL`,
    `PhoneNumber NVARCHAR(20) NOT NULL`, `Email NVARCHAR(200) NOT NULL`. Add
    `CK_Patients_Gender CHECK (Gender IN (0,1,2))` alongside the existing
    `CK_Patients_Nhi`/`CK_Providers_Type` check constraints.
  - `tblProviders`: add `Email NVARCHAR(200) NOT NULL`, `PhoneNumber NVARCHAR(20) NOT NULL`,
    `Qualification NVARCHAR(200) NOT NULL`.
- Stored procs (same pattern in each: add params, extend INSERT/SELECT column list, leave
  the existing TRY/CATCH → `TblErrorLog` block untouched):
  - `StoredProcedures/Profile/usp_Patient_Create.sql`, `usp_Patient_GetById.sql`,
    `usp_Patient_Search.sql`
  - `StoredProcedures/Profile/usp_Provider_Create.sql`, `usp_Provider_GetById.sql`,
    `usp_Provider_List.sql`
- `Performance/01_ExpandLookupData.sql` - the bulk perf-data INSERT statements for
  `Profile.tblProviders` (line ~63) and `Profile.tblPatients` (line ~81) use explicit
  column lists with no defaults for the new NOT NULL columns, so they must generate values
  too (e.g. deterministic fake DOB/gender/phone/email/qualification from `N`, same style as
  the existing deterministic NHI generation).

### Backend (`Backend/ScriptFlow.API`)
- `Shared.contract/Enums/Gender.cs` - new enum file, mirrors `ProviderType.cs`.
- `Domain/Entities/Patient.cs` - add 4 properties + constructor params + validation
  (required checks + DOB-not-in-future).
- `Domain/Entities/Provider.cs` - add 3 properties + constructor params + required checks.
- `Application/Commands/CreatePatientCommand.cs` / `CreateProviderCommand.cs` - add fields.
- `Application/Validators/CreatePatientCommandValidator.cs` /
  `CreateProviderCommandValidator.cs` - add the rules from Design decisions above.
- `Application/DTOs/PatientDto.cs` / `ProviderDto.cs` - add fields.
- `Application/Mappings/MappingExtensions.cs` - extend both `ToDto()` overloads.
- `Application/Handlers/CreatePatientCommandHandler.cs` /
  `CreateProviderCommandHandler.cs` - pass new fields into the entity constructor.
- `Infrastructure/Persistence/SqlPatientRepository.cs` / `SqlProviderRepository.cs` -
  extend the private `Row` records, `ToEntity`, and the `AddAsync` anonymous parameter
  object.
- No controller changes needed (they already bind the whole command from the body).

### Tests (`Backend/ScriptFlow.API.Tests`)
- `Domain/Entities/PatientTests.cs` / `ProviderTests.cs` - update all `new Patient(...)` /
  `new Provider(...)` call sites (5 each) with the new constructor args; add 1-2 new test
  cases for the added validation (DOB-in-future rejected, etc.), matching the existing
  `[Theory]`/`Assert.Throws<DomainException>` style already in these files.
- `Integration/PrimaryWorkflowIntegrationTests.cs` - add the 4 new fields to the
  `/api/patients` POST body (line ~76) so the skippable end-to-end test still passes when
  real infra is reachable.

### Frontend (`Frontend/ScriptFlow-UI/src/app`)
- `shared/models/gender.ts` - new file, mirrors `shared/models/provider-type.ts`.
- `core/models/patient.model.ts` - add `dateOfBirth`, `gender`, `phoneNumber`, `email` to
  both `Patient` and `CreatePatientRequest`.
- `core/models/provider.model.ts` - add `email`, `phoneNumber`, `qualification` to both
  `Provider` and `CreateProviderRequest`.
- `features/patients/patient-form/patient-form.component.ts` - add 4 `FormControl`s with
  validators (`Validators.required`, `Validators.email` for email, `Validators.pattern`
  for phone) + getters, matching the existing `firstNameControl`-style getters.
- `features/patients/patient-form/patient-form.component.html` - add
  `<app-text-field type="date">` for DOB, `<app-select-field>` (options = `GENDERS`) for
  gender, `<app-text-field>` for phone, `<app-text-field type="email">` for email -
  reusing `TextFieldComponent`/`SelectFieldComponent` exactly as the existing fields do.
- `features/providers/provider-form/provider-form.component.ts` /`.html` - same treatment
  for email, phone, qualification, reusing the existing `PROVIDER_TYPES`/`typeOptions`
  select pattern for structure (qualification is a plain text field, no new select needed).
- `features/patients/patient-detail/patient-detail.component.html` /
  `features/providers/provider-detail/provider-detail.component.html` - add the new facts
  to the existing `.profile-facts` block (same `<span class="fact"><app-icon .../> ...`
  pattern already used for NHI/address and type/NZMC), so newly-entered data is visible
  somewhere after saving, not just written and never shown.
- Out of scope: patient-search and provider-list pages (they only need name/NHI/type for
  their current job - picking a patient/provider for a prescription - so left untouched to
  keep the change focused).

## Verification
- `dotnet build` and `dotnet test` in `Backend/ScriptFlow.API.Tests` (unit tests must not
  need real infra; integration test self-skips without SQL Server/RabbitMQ reachable).
- `npm run build` in `Frontend/ScriptFlow-UI` to confirm the Angular app still compiles.
- If a local SQL Server is reachable: re-run `00_CreateSchema.sql` + the updated stored
  procs against a scratch DB, then manually submit both forms in the running UI and
  confirm the new fields round-trip through create → detail view.
