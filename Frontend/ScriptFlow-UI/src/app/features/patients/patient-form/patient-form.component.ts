import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PatientService } from '../../../core/services/patient.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { GENDERS, Gender } from '../../../shared/models/gender';

// Matches CreatePatientCommandValidator: NHI is 3 letters followed by 4 digits, e.g. ABC1234.
const NHI_PATTERN = /^[A-Za-z]{3}[0-9]{4}$/;
// Matches CreatePatientCommandValidator's phone number rule.
const PHONE_PATTERN = /^[0-9+\-\s()]{7,20}$/;

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonComponent,
    TextFieldComponent,
    SelectFieldComponent,
    IconComponent,
  ],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.css',
})
export class PatientFormComponent {
  private readonly patientService = inject(PatientService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly genderOptions: SelectOption[] = GENDERS.map((gender) => ({ value: gender, label: gender }));

  readonly form = new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nhi: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(NHI_PATTERN)],
    }),
    dateOfBirth: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    gender: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(PHONE_PATTERN)],
    }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  get firstNameControl(): FormControl<string> {
    return this.form.controls.firstName;
  }

  get lastNameControl(): FormControl<string> {
    return this.form.controls.lastName;
  }

  get addressControl(): FormControl<string> {
    return this.form.controls.address;
  }

  get nhiControl(): FormControl<string> {
    return this.form.controls.nhi;
  }

  get dateOfBirthControl(): FormControl<string> {
    return this.form.controls.dateOfBirth;
  }

  get genderControl(): FormControl<string> {
    return this.form.controls.gender;
  }

  get phoneNumberControl(): FormControl<string> {
    return this.form.controls.phoneNumber;
  }

  get emailControl(): FormControl<string> {
    return this.form.controls.email;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.patientService
      .create({ ...value, gender: value.gender as Gender })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((patient) => {
        this.notifications.success(`${patient.firstName} ${patient.lastName} was added.`);
        this.router.navigate(['/patients', patient.id]);
      });
  }
}
