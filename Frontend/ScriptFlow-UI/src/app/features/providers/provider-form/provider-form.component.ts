import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ProviderService } from '../../../core/services/provider.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PROVIDER_TYPES, ProviderType } from '../../../shared/models/provider-type';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

// Matches CreateProviderCommandValidator's phone number rule.
const PHONE_PATTERN = /^[0-9+\-\s()]{7,20}$/;

@Component({
  selector: 'app-provider-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonComponent, TextFieldComponent, SelectFieldComponent, IconComponent],
  templateUrl: './provider-form.component.html',
  styleUrl: './provider-form.component.css',
})
export class ProviderFormComponent {
  private readonly providerService = inject(ProviderService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly practiceLocationOptions = signal<SelectOption[]>([]);
  readonly typeOptions: SelectOption[] = PROVIDER_TYPES.map((type) => ({ value: type, label: type }));

  readonly form = new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    type: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nzmcNo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    practiceLocationId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(PHONE_PATTERN)],
    }),
    qualification: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.practiceLocationService.list().subscribe({
      next: (locations) => {
        this.practiceLocationOptions.set(
          locations.map((location) => ({ value: location.id, label: `${location.name} (${location.hpiNumber})` })),
        );
      },
      // errorInterceptor already toasts the failure; catching it here just stops it from
      // being an uncaught RxJS error and leaves the dropdown empty instead of stuck.
      error: () => this.practiceLocationOptions.set([]),
    });
  }

  get firstNameControl(): FormControl<string> {
    return this.form.controls.firstName;
  }

  get lastNameControl(): FormControl<string> {
    return this.form.controls.lastName;
  }

  get typeControl(): FormControl<string> {
    return this.form.controls.type;
  }

  get nzmcNoControl(): FormControl<string> {
    return this.form.controls.nzmcNo;
  }

  get practiceLocationControl(): FormControl<string> {
    return this.form.controls.practiceLocationId;
  }

  get emailControl(): FormControl<string> {
    return this.form.controls.email;
  }

  get phoneNumberControl(): FormControl<string> {
    return this.form.controls.phoneNumber;
  }

  get qualificationControl(): FormControl<string> {
    return this.form.controls.qualification;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.providerService
      .create({ ...value, type: value.type as ProviderType })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((provider) => {
        this.notifications.success(`Dr ${provider.firstName} ${provider.lastName} was added.`);
        this.router.navigate(['/providers', provider.id]);
      });
  }
}
