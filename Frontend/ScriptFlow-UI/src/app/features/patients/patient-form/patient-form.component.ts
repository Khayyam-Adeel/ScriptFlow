import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { PatientService } from '../../../core/services/patient.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';

// Matches CreatePatientCommandValidator: NHI is 3 letters followed by 4 digits, e.g. ABC1234.
const NHI_PATTERN = /^[A-Za-z]{3}[0-9]{4}$/;

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, TextFieldComponent],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.css',
})
export class PatientFormComponent {
  private readonly patientService = inject(PatientService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);

  readonly form = new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nhi: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(NHI_PATTERN)],
    }),
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

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.patientService
      .create(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((patient) => {
        this.notifications.success(`${patient.firstName} ${patient.lastName} was added.`);
        this.router.navigate(['/patients', patient.id]);
      });
  }
}
