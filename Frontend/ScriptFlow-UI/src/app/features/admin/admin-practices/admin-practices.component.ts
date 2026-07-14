import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { PracticeService } from '../../../core/services/practice.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Practice } from '../../../core/models/practice.model';
import { PracticeLocation } from '../../../core/models/practice-location.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

interface PracticeGroup {
  practice: Practice;
  locations: PracticeLocation[];
}

// Matches CreatePracticeLocationCommandValidator's phone number rule.
const PHONE_PATTERN = /^[0-9+\-\s()]{7,20}$/;

/** Practices & locations management: read side groups locations under their practice; write
 * side opens a small modal form for each (both Create endpoints are Admin-only, matching this
 * page's own adminGuard-protected route). */
@Component({
  selector: 'app-admin-practices',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonComponent,
    TextFieldComponent,
    SelectFieldComponent,
    IconComponent,
    SpinnerComponent,
    ModalComponent,
  ],
  templateUrl: './admin-practices.component.html',
  styleUrl: './admin-practices.component.css',
})
export class AdminPracticesComponent {
  private readonly practiceService = inject(PracticeService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly notifications = inject(NotificationService);

  readonly loading = signal(true);
  readonly practices = signal<Practice[]>([]);
  readonly locations = signal<PracticeLocation[]>([]);

  readonly practiceGroups = computed<PracticeGroup[]>(() =>
    this.practices().map((practice) => ({
      practice,
      locations: this.locations().filter((location) => location.practiceId === practice.id),
    })),
  );

  readonly practiceOptions = computed<SelectOption[]>(() =>
    this.practices().map((practice) => ({ value: practice.id, label: practice.name })),
  );

  readonly showNewPracticeModal = signal(false);
  readonly showNewLocationModal = signal(false);
  readonly submitting = signal(false);

  readonly newPracticeForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  readonly newLocationForm = new FormGroup({
    practiceId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    hpiNo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z]{3}[0-9]{2}$/)],
    }),
    hpiExtension: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z]$/)],
    }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(PHONE_PATTERN)],
    }),
  });

  get newPracticeNameControl(): FormControl<string> {
    return this.newPracticeForm.controls.name;
  }

  get newLocationPracticeControl(): FormControl<string> {
    return this.newLocationForm.controls.practiceId;
  }

  get newLocationNameControl(): FormControl<string> {
    return this.newLocationForm.controls.name;
  }

  get newLocationHpiNoControl(): FormControl<string> {
    return this.newLocationForm.controls.hpiNo;
  }

  get newLocationHpiExtensionControl(): FormControl<string> {
    return this.newLocationForm.controls.hpiExtension;
  }

  get newLocationAddressControl(): FormControl<string> {
    return this.newLocationForm.controls.address;
  }

  get newLocationPhoneControl(): FormControl<string> {
    return this.newLocationForm.controls.phone;
  }

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    forkJoin({
      practices: this.practiceService.list(),
      locations: this.practiceLocationService.list(),
    }).subscribe(({ practices, locations }) => {
      this.practices.set(practices);
      this.locations.set(locations);
      this.loading.set(false);
    });
  }

  openNewPracticeModal(): void {
    this.newPracticeForm.reset({ name: '' });
    this.showNewPracticeModal.set(true);
  }

  openNewLocationModal(): void {
    this.newLocationForm.reset({
      practiceId: '',
      name: '',
      hpiNo: '',
      hpiExtension: '',
      address: '',
      phone: '',
    });
    this.showNewLocationModal.set(true);
  }

  submitNewPractice(): void {
    this.newPracticeForm.markAllAsTouched();
    if (this.newPracticeForm.invalid) {
      return;
    }

    this.submitting.set(true);
    this.practiceService
      .create(this.newPracticeForm.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((practice) => {
        this.notifications.success(`${practice.name} was added.`);
        this.showNewPracticeModal.set(false);
        this.load();
      });
  }

  submitNewLocation(): void {
    this.newLocationForm.markAllAsTouched();
    if (this.newLocationForm.invalid) {
      return;
    }

    this.submitting.set(true);
    this.practiceLocationService
      .create(this.newLocationForm.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((location) => {
        this.notifications.success(`${location.name} was added.`);
        this.showNewLocationModal.set(false);
        this.load();
      });
  }
}
