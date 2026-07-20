# Prescription print preview

**Status: Implemented.** Verified in code — `shared/components/barcode/barcode.component.ts`
(from-scratch Code128 encoder), `shared/components/modal/modal.component.ts`, and
`features/prescriptions/prescription-print/` all exist, wired into both the
post-create flow and a "Print" button on the prescription detail page.

## Context

The user wants a print preview to pop up whenever a prescription is created, before it's
signed, styled after a real NZ prescribing-software printout (reference screenshot:
`C:\Users\khayyam.adeel\Downloads\Capture 1.PNG` — an "indici" system prescription print).
Confirmed with the user:
- Barcode: a **real scannable Code128 barcode** encoding the prescription SCID (no barcode
  library exists in this project today — implemented from scratch, matching the project's
  existing convention of hand-authored inline SVGs instead of pulling in graphics libraries,
  see `shared/components/icon/icon.component.ts`).
- The reference shows practice address/phone that ScriptFlow's `PracticeLocation` doesn't
  model yet — **add Address/Phone to PracticeLocation first** (same full-stack layered
  pattern already used for the recent Patient/Provider field additions).
- Presentation: a **pop-up/modal** print preview (not a separate routed page), shown right
  after creation, reusable afterward as a "Print" button on the prescription detail page.

Reference layout maps onto ScriptFlow's actual data like this:

| Reference element | ScriptFlow source |
|---|---|
| Practice name / HPI Facility | `PracticeLocation.name` / `.hpiNumber` |
| Practice address / phone | `PracticeLocation.address` / `.phone` (**new**) |
| "Dr {name}" | `Provider.firstName/lastName`, title derived from `Provider.type` |
| Reg Auth No. | `Provider.nzmcNo` |
| Item Count | `prescription.medications.length` |
| Patient name (large) | `Patient.firstName/lastName` |
| DOB / NHI / Gender / Phone | `Patient.dateOfBirth/nhi/gender/phoneNumber` (already added) |
| Barcode + code text | `Prescription.scid`, Code128-encoded |
| RX {datetime} | `Prescription.createdAtUtc` |
| Medication blocks (name, Sig, Mitte) | `Medication.medicineName/takeValue/frequency/duration/quantity`, unit from `Medicine.form` |
| Footer signature line | `Provider` full name + blank line, "Page 1 of 1" |

Fields the reference shows that ScriptFlow doesn't model (GMS, CPN, home vs mobile phone
split) are omitted — only real data is printed, no fabricated placeholders.

## Part A — Add Address/Phone to PracticeLocation

Same layered pattern as the earlier Patient/Provider work; `PracticeLocation` already has a
(currently unused-by-any-controller) `Create` path wired end-to-end, so this follows its
existing shape exactly:

- **DB**: `Schema/00_CreateSchema.sql` — add `Address NVARCHAR(500) NOT NULL`,
  `Phone NVARCHAR(20) NOT NULL` to `Admin.tblPracticeLocations`.
- **Stored procs**: `StoredProcedures/Admin/usp_PracticeLocation_Create.sql` (add params +
  insert columns), `usp_PracticeLocation_GetById.sql`, `usp_PracticeLocation_List.sql` (add
  to SELECT).
- **Backend**: `Domain/Entities/PracticeLocation.cs` (add properties + required-field
  validation, same style as `Patient.cs`), `Application/DTOs/PracticeLocationDto.cs`,
  `Application/Mappings/MappingExtensions.cs` (`ToDto`), `Infrastructure/Persistence/
  SqlPracticeLocationRepository.cs` (Row record, `ToEntity`, `AddAsync` params).
- **Tests**: `ScriptFlow.API.Tests/Domain/Entities/PracticeLocationTests.cs` — update the 4
  constructor call sites.
- **Seed data**: `Performance/01_ExpandLookupData.sql`'s practice-location INSERT needs
  deterministic Address/Phone values added (same style as its existing deterministic
  HpiNo generation).
- **Frontend**: `core/models/practice-location.model.ts` — add `address`, `phone`.

## Part B — Real Code128 barcode (no external library)

- `shared/utils/code128.ts` — pure encoder, Subset B only (sufficient: SCIDs are uppercase
  letters + digits, all within ASCII 32-127). Exports `encodeCode128(value: string): number[]`
  (symbol values: START_B, one per character, checksum, STOP) and the standard 107-entry
  bar/space width pattern table, converted to an array of module widths for rendering.
- `shared/components/barcode/barcode.component.ts` — `@Input value: string`, `@Input
  height = 50`, `@Input showText = true`. Renders the encoded pattern as `<svg><rect>` bars
  (black bars on white, module width scaled to fit), with the human-readable `value` text
  underneath (matches the reference image, and doubles as a fallback if a bar is ever
  misrendered).
- **Caveat to flag to the user**: this is a from-scratch encoder against the published
  Code128 spec: logically correct, but I have no physical scanner to verify against, so it's
  worth test-scanning the first printed barcode with a phone barcode-scanner app to confirm
  before relying on it operationally.

## Part C — Print preview modal + wiring

- `shared/components/modal/modal.component.ts` — new generic reusable overlay+panel
  (content projection via `<ng-content>`, `@Output closed`, closes on backdrop click / Esc /
  close button). No modal primitive exists yet in `shared/components/`; this is the first.
- `features/prescriptions/prescription-print/prescription-print.component.ts` — takes
  `@Input({ required: true }) prescriptionId: string`. Internally fetches the prescription
  (`PrescriptionService.getById`), patient, provider (existing services), practice location
  (via `PracticeLocationService.list()`'s already-cached observable, found by id — no new
  endpoint needed), and the full medicine list (`MedicineService.list()`, for `form` per
  medicine, mirroring the `medicineNames` map pattern already in `prescription-form.component.ts`).
  Renders the print-sheet layout described in the Context table, including the new
  `<app-barcode [value]="prescription.scid">`.
  - Print CSS: `@media print` rule (scoped, vanilla CSS, no library) hides everything except
    `.print-sheet` and removes the modal chrome, so `window.print()` (triggered by an
    in-modal "Print" button) only prints the prescription content.
  - A "Continue to sign" / close button in the modal footer (screen-only, hidden when printing).
- **Wiring into `prescription-form.component.ts`**: in `submit()`'s create branch (not edit),
  on success, instead of navigating straight to `/prescriptions/:id`, open the modal with the
  new prescription's id; navigating to the detail page happens when the modal is closed.
- **Wiring into `prescription-detail.component.ts`**: add a "Print" button in `.page-actions`
  (next to Edit/Sign/Repeat) that opens the same modal for reprinting at any time after creation.

## Verification
- `dotnet build`/`dotnet test` after Part A (unit tests + the skippable integration test).
- Apply the same local-dev-DB migration pattern used for the earlier Patient/Provider fields
  (`ALTER TABLE` + redeploy the 3 touched stored procs) so the running local API actually
  reflects the new PracticeLocation columns.
- `npm run build` for the frontend.
- Manually exercise in the browser: create a prescription → confirm the print modal opens
  automatically with real patient/provider/practice/medication data and a barcode → click
  Print → verify `window.print()` preview shows only the print-sheet → close/continue →
  confirm it lands on the prescription detail page in `Created` status (not yet signed) →
  click the new "Print" button on an existing prescription to confirm reprint works too.
