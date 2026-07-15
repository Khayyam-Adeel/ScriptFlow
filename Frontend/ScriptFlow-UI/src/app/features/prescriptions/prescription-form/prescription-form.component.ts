import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil } from 'rxjs';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { PatientService } from '../../../core/services/patient.service';
import { ProviderService } from '../../../core/services/provider.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { MedicineService } from '../../../core/services/medicine.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Patient } from '../../../core/models/patient.model';
import { Medicine } from '../../../core/models/medicine.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { PrescriptionPrintComponent } from '../prescription-print/prescription-print.component';

interface MedicationLineGroup {
  medicineId: FormControl<string>;
  strength: FormControl<string>;
  route: FormControl<string>;
  takeValue: FormControl<string>;
  frequency: FormControl<string>;
  duration: FormControl<string>;
  quantity: FormControl<number>;
  isPrn: FormControl<boolean>;
  directions: FormControl<string>;
  notes: FormControl<string>;
  repeats: FormControl<number>;
}

/** Common administration routes; the backend column is free text, so "Other" isn't needed —
 * anything here is just a convenience shortlist. */
export const ROUTE_OPTIONS: SelectOption[] = [
  'Oral',
  'Sublingual',
  'Topical',
  'Inhaled',
  'Nasal',
  'Ophthalmic',
  'Otic',
  'Rectal',
  'Subcutaneous',
  'Intramuscular',
  'Intravenous',
].map((route) => ({ value: route, label: route }));

/** Whole numbers only - matches MedicationLineValidator.cs, which rejects fractional
 * Quantity/Repeats at the API, so catch it client-side too rather than round-tripping. */
const INTEGER_PATTERN = /^[0-9]+$/;

/** TakeValue/Duration are free text ("1 tablet", "7 days") - MedicineLine.TakeValue/Duration
 * are NVARCHAR(100), so the unit is never split out. Require a leading whole number so the
 * dose/day count itself is still structured, and leave the rest (unit, wording) as free string. */
const LEADING_INTEGER_PATTERN = /^[0-9]+\D*$/;

function buildMedicationLine(): FormGroup<MedicationLineGroup> {
  return new FormGroup<MedicationLineGroup>({
    medicineId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    strength: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    route: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    takeValue: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(LEADING_INTEGER_PATTERN)],
    }),
    frequency: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    duration: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(LEADING_INTEGER_PATTERN)],
    }),
    quantity: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(INTEGER_PATTERN), Validators.min(1)],
    }),
    isPrn: new FormControl(false, { nonNullable: true }),
    directions: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }),
    repeats: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(INTEGER_PATTERN), Validators.min(0)],
    }),
  });
}

/** The raw value of one medication FormGroup — snapshotted into a signal to drive the timeline. */
interface MedicationRaw {
  medicineId: string;
  strength: string;
  route: string;
  takeValue: string;
  frequency: string;
  duration: string;
  quantity: number;
  isPrn: boolean;
  directions: string;
  notes: string;
  repeats: number;
}

/** One entry rendered in the live "medications so far" timeline beside the form. */
export interface TimelineEntry {
  index: number;
  medicineName: string;
  strength: string;
  route: string;
  takeValue: string;
  frequency: string;
  duration: string;
  quantity: number;
  isPrn: boolean;
  notes: string;
  repeats: number;
}

/**
 * Create and update prescriptions share one form: in edit mode (route has :id) the patient,
 * provider, and practice location are fixed (UpdatePrescriptionCommand only changes
 * medications), matching Prescription.cs's EnsureStatus(Created, "update") rule.
 */
@Component({
  selector: 'app-prescription-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonComponent,
    TextFieldComponent,
    SelectFieldComponent,
    SpinnerComponent,
    IconComponent,
    ModalComponent,
    PrescriptionPrintComponent,
  ],
  templateUrl: './prescription-form.component.html',
  styleUrl: './prescription-form.component.css',
})
export class PrescriptionFormComponent implements OnDestroy {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly patientService = inject(PatientService);
  private readonly providerService = inject(ProviderService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly medicineService = inject(MedicineService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyed$ = new Subject<void>();

  readonly editingId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = this.editingId !== null;
  readonly loading = signal(this.isEditMode);
  readonly submitting = signal(false);

  /** Set once a prescription is created, so the print preview modal opens before the
   * prescriber is taken to the detail page where they'd sign it. */
  readonly createdPrescriptionId = signal<string | null>(null);

  readonly providerOptions = signal<SelectOption[]>([]);
  readonly practiceLocationOptions = signal<SelectOption[]>([]);
  readonly medicineOptions = signal<SelectOption[]>([]);
  readonly routeOptions = ROUTE_OPTIONS;

  private readonly medicineNames = signal<Map<string, string>>(new Map());
  private readonly medicationsSnapshot = signal<MedicationRaw[]>([]);

  /** The medication lines filled in so far (a medicine has been chosen), shaped for the live
   * timeline panel beside the form. Recomputes as the prescriber types or loads an existing Rx. */
  readonly timeline = computed<TimelineEntry[]>(() => {
    const names = this.medicineNames();
    return this.medicationsSnapshot()
      .map((line, index) => ({
        index,
        medicineName: names.get(line.medicineId) ?? '',
        strength: line.strength,
        route: line.route,
        takeValue: line.takeValue,
        frequency: line.frequency,
        duration: line.duration,
        quantity: line.quantity,
        isPrn: line.isPrn,
        notes: line.notes,
        repeats: line.repeats,
      }))
      .filter((entry) => !!entry.medicineName);
  });

  readonly patientQuery = new FormControl('', { nonNullable: true });
  readonly patientResults = signal<Patient[]>([]);
  readonly selectedPatient = signal<Patient | null>(null);

  readonly form = new FormGroup({
    providerId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    practiceLocationId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    medications: new FormArray<FormGroup<MedicationLineGroup>>([buildMedicationLine()]),
  });

  get providerControl(): FormControl<string> {
    return this.form.controls.providerId;
  }

  get practiceLocationControl(): FormControl<string> {
    return this.form.controls.practiceLocationId;
  }

  get medications(): FormArray<FormGroup<MedicationLineGroup>> {
    return this.form.controls.medications;
  }

  constructor() {
    this.providerService.list().subscribe((providers) => {
      this.providerOptions.set(
        providers.map((p) => ({ value: p.id, label: `${p.firstName} ${p.lastName} (${p.type})` })),
      );
    });

    this.practiceLocationService.list().subscribe((locations) => {
      this.practiceLocationOptions.set(
        locations.map((l) => ({ value: l.id, label: `${l.name} (${l.hpiNumber})` })),
      );
    });

    this.medicineService.list().subscribe((medicines: Medicine[]) => {
      this.medicineOptions.set(medicines.map((m) => ({ value: m.id, label: `${m.name} (${m.form})` })));
      this.medicineNames.set(new Map(medicines.map((m) => [m.id, m.name])));
    });

    // Mirror the medication FormArray into a signal so the timeline panel re-renders live as
    // lines are edited, added, removed, or patched in from an existing prescription.
    this.medicationsSnapshot.set(this.medications.getRawValue());
    this.medications.valueChanges
      .pipe(takeUntil(this.destroyed$))
      .subscribe(() => this.medicationsSnapshot.set(this.medications.getRawValue()));

    this.patientQuery.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((query) => {
          const trimmed = query.trim();
          return trimmed ? this.patientService.search(trimmed) : [];
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe((patients) => this.patientResults.set(patients));

    const queryPatientId = this.route.snapshot.queryParamMap.get('patientId');
    if (queryPatientId) {
      this.patientService.getById(queryPatientId).subscribe((patient) => this.selectedPatient.set(patient));
    }

    if (this.isEditMode && this.editingId) {
      this.prescriptionService
        .getById(this.editingId)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((prescription) => {
          this.patientService.getById(prescription.patientId).subscribe((patient) => this.selectedPatient.set(patient));
          this.form.patchValue({
            providerId: prescription.providerId,
            practiceLocationId: prescription.practiceLocationId,
          });

          this.medications.clear();
          for (const medication of prescription.medications) {
            const line = buildMedicationLine();
            // The optional fields are nullable on the DTO but the controls are non-nullable, so
            // coalesce null -> '' rather than pushing null into a nonNullable FormControl.
            line.patchValue({
              medicineId: medication.medicineId,
              strength: medication.strength ?? '',
              route: medication.route ?? '',
              takeValue: medication.takeValue,
              frequency: medication.frequency,
              duration: medication.duration,
              quantity: medication.quantity,
              isPrn: medication.isPrn,
              directions: medication.directions,
              notes: medication.notes ?? '',
              repeats: medication.repeats,
            });
            this.medications.push(line);
          }

          // UpdatePrescriptionCommand only carries medications, so patient/provider/location
          // are fixed once a prescription exists — disable rather than silently ignore edits.
          this.providerControl.disable();
          this.practiceLocationControl.disable();
        });
    }
  }

  selectPatient(patient: Patient): void {
    this.selectedPatient.set(patient);
    this.patientQuery.setValue('');
    this.patientResults.set([]);
  }

  clearSelectedPatient(): void {
    this.selectedPatient.set(null);
  }

  addMedicationLine(): void {
    this.medications.push(buildMedicationLine());
  }

  removeMedicationLine(index: number): void {
    if (this.medications.length > 1) {
      this.medications.removeAt(index);
    }
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.selectedPatient()) {
      if (!this.selectedPatient()) {
        this.notifications.error('Select a patient before saving.');
      }
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);

    if (this.isEditMode && this.editingId) {
      this.prescriptionService
        .update(this.editingId, { medications: value.medications })
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe((prescription) => {
          this.notifications.success('Prescription updated.');
          this.router.navigate(['/prescriptions', prescription.id]);
        });
    } else {
      this.prescriptionService
        .create({
          patientId: this.selectedPatient()!.id,
          providerId: value.providerId,
          practiceLocationId: value.practiceLocationId,
          medications: value.medications,
        })
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe((prescription) => {
          this.notifications.success(`Prescription ${prescription.scid} created.`);
          // Show the print preview before handing off to the detail page (where signing
          // happens) - goToDetail() below navigates once the prescriber closes it.
          this.createdPrescriptionId.set(prescription.id);
        });
    }
  }

  goToDetail(): void {
    const id = this.createdPrescriptionId();
    if (id) {
      this.router.navigate(['/prescriptions', id]);
    }
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
