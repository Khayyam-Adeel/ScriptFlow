import { Component, OnDestroy, inject, signal } from '@angular/core';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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

interface MedicationLineGroup {
  medicineId: FormControl<string>;
  takeValue: FormControl<string>;
  frequency: FormControl<string>;
  duration: FormControl<string>;
  quantity: FormControl<number>;
  directions: FormControl<string>;
}

function buildMedicationLine(): FormGroup<MedicationLineGroup> {
  return new FormGroup<MedicationLineGroup>({
    medicineId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    takeValue: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    frequency: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    duration: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    directions: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
}

/**
 * Create and update prescriptions share one form: in edit mode (route has :id) the patient,
 * provider, and practice location are fixed (UpdatePrescriptionCommand only changes
 * medications), matching Prescription.cs's EnsureStatus(Created, "update") rule.
 */
@Component({
  selector: 'app-prescription-form',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, TextFieldComponent, SelectFieldComponent, SpinnerComponent],
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

  readonly providerOptions = signal<SelectOption[]>([]);
  readonly practiceLocationOptions = signal<SelectOption[]>([]);
  readonly medicineOptions = signal<SelectOption[]>([]);

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
    });

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
            line.patchValue(medication);
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
          this.router.navigate(['/prescriptions', prescription.id]);
        });
    }
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
