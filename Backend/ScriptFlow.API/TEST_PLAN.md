# Test project + domain-layer unit tests for ScriptFlow.API

## Context

The assignment requires automated tests covering >=70% of the domain layer plus at
least one integration test of the primary workflow. A full audit of the repo found
**zero test projects anywhere** in `ScriptFlow.sln` - this is the single biggest gap
against the rubric. This is step 1 of fixing that: stand up a test project and write
unit tests for `ScriptFlow.API`'s Domain layer (the only project with a real Domain
folder: entities, value objects, exceptions). Integration tests and Application-layer
tests are separate, later steps - out of scope here.

## What's being tested

`Backend/ScriptFlow.API/Domain/` contains 15 files with real business rules to verify:

- **Entities** (`Entities/`): `Prescription` (the aggregate root - state machine
  `Created->Signed->Acknowledged/Rejected`, guard-clause exceptions on invalid
  transitions, `Repeat()` cloning rules, `UpdateMedications` only-when-`Created`
  rule, empty-medication-list rejection), `PrescriptionMedication`, `Patient`,
  `Provider`, `Practice`, `PracticeLocation`, `Medicine` (all constructor guard
  clauses), `SystemUser` (trivial constant), `User` (email normalization + guard
  clauses).
- **Value objects** (`ValueObjects/`): `Scid` (regex format validation + `Generate()`),
  `Nhi` (format validation + normalization), `HpiNumber` (two-part validation +
  `Parse()`).
- **Exceptions** (`Exceptions/`): `DomainException`, `EntityNotFoundException`,
  `InvalidPrescriptionStateException` - thin, but covered incidentally through the
  entity tests above that trigger them; no dedicated tests needed beyond that.

`Prescription.Rehydrate(...)` is `internal static` (only SQL repositories call it) and
takes a different code path than the public constructor (skips the
`RequireNonEmpty` check). To cover it without making it public, add one
`InternalsVisibleTo` entry to `ScriptFlow.API`.

## Approach

**Stack:** xUnit + `coverlet.collector` (the standard `dotnet new xunit` template).
No FluentAssertions/Shouldly - xUnit's built-in `Assert` is enough here and avoids
pulling in a third-party assertion library (FluentAssertions v8+ changed to a paid
license for commercial use). Coverlet is chosen because it plugs into `dotnet test
--collect:"XPlat Code Coverage"`, which is what GitHub Actions will run in the CI
step later (a future, separate piece of work).

**New project:** `Backend/ScriptFlow.API.Tests/ScriptFlow.API.Tests.csproj`
- Scaffolded via `dotnet new xunit -n ScriptFlow.API.Tests -o Backend/ScriptFlow.API.Tests -f net8.0`.
- `Nullable`/`ImplicitUsings` set to `enable`, matching `ScriptFlow.API.csproj`'s convention.
- `ProjectReference` to `Backend/ScriptFlow.API/ScriptFlow.API.csproj`.
- Added to `ScriptFlow.sln` via `dotnet sln add`.

**New file in `ScriptFlow.API`:** a small `AssemblyInfo.cs` with
`[assembly: InternalsVisibleTo("ScriptFlow.API.Tests")]`, so tests can reach
`Prescription.Rehydrate`.

**Test files** (mirroring the Domain folder structure, one class per source type):
```
Backend/ScriptFlow.API.Tests/
  Domain/
    Entities/
      PrescriptionTests.cs        (state machine, Repeat, UpdateMedications, Rehydrate)
      PrescriptionMedicationTests.cs
      PatientTests.cs
      ProviderTests.cs
      PracticeTests.cs
      PracticeLocationTests.cs
      MedicineTests.cs
      UserTests.cs
    ValueObjects/
      ScidTests.cs
      NhiTests.cs
      HpiNumberTests.cs
```
Each test class covers: valid construction succeeds and assigns properties correctly;
each guard clause throws `DomainException`/`ArgumentNullException` with invalid
input (null/whitespace/empty Guid/zero-or-negative quantity, as applicable); for
`Prescription`, every legal and illegal state transition (e.g. `Sign()` twice throws,
`Acknowledge()`/`Reject()` only valid from `Signed`, `Repeat()` only valid from
`Signed`/`Dispatched`/`Acknowledged`, `UpdateMedications` throws once not `Created`).
Uses `[Theory]`/`[InlineData]` for the repeated "throws on blank string" cases across
entities to avoid copy-pasted near-duplicate test methods.

## Verification

1. `dotnet build ScriptFlow.sln` - confirm the new project builds and the solution
   still builds clean.
2. `dotnet test Backend/ScriptFlow.API.Tests --collect:"XPlat Code Coverage"` - all
   tests pass.
3. Check the domain-layer coverage percentage from the generated Cobertura report
   (via a one-off `dotnet tool install -g dotnet-reportgenerator-globaltool` +
   `reportgenerator` run against the `coverage.cobertura.xml`, filtered to the
   `ScriptFlow.API.Domain.*` classes) - confirm it clears the >=70% target; add any
   missing edge-case tests if a class falls short.

## Status

- [x] Scaffold test project + add to solution (`Backend/ScriptFlow.API.Tests`)
- [x] AssemblyInfo.cs InternalsVisibleTo
- [x] Write entity tests
- [x] Write value object tests
- [x] Run tests + measure coverage

**Result:** 127 tests, all passing. Domain-layer line coverage: 98.7% (target was
>=70%). Branch coverage 97.9%. Verified via:
```
dotnet test Backend/ScriptFlow.API.Tests --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:TestResults/report -reporttypes:TextSummary -classfilters:"+ScriptFlow.API.Domain.*"
```
(`reportgenerator` requires a one-off `dotnet tool install -g dotnet-reportgenerator-globaltool`.)

Next steps for the broader "automated tests" requirement (not done here): Application-layer
tests (handlers/validators) and at least one integration test of the primary
create -> sign -> dispatch -> acknowledge workflow.
