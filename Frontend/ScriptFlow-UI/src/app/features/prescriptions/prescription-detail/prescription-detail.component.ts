import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { PatientService } from '../../../core/services/patient.service';
import { ProviderService } from '../../../core/services/provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Prescription } from '../../../core/models/prescription.model';
import { Patient } from '../../../core/models/patient.model';
import { Provider } from '../../../core/models/provider.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

/**
 * Sign/Repeat/Edit availability mirrors Prescription.cs's state machine exactly, so this UI
 * never offers an action the API would reject with 409 Conflict.
 */
@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, ButtonComponent, StatusBadgeComponent, SpinnerComponent],
  templateUrl: './prescription-detail.component.html',
  styleUrl: './prescription-detail.component.css',
})
export class PrescriptionDetailComponent {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly patientService = inject(PatientService);
  private readonly providerService = inject(ProviderService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly prescriptionId = this.route.snapshot.paramMap.get('id')!;

  readonly prescription = signal<Prescription | null>(null);
  readonly patient = signal<Patient | null>(null);
  readonly provider = signal<Provider | null>(null);
  readonly actionInFlight = signal(false);

  get canSign(): boolean {
    return this.prescription()?.status === 'Created';
  }

  get canEdit(): boolean {
    return this.prescription()?.status === 'Created';
  }

  get canRepeat(): boolean {
    const status = this.prescription()?.status;
    return status === 'Signed' || status === 'Dispatched' || status === 'Acknowledged';
  }

  constructor() {
    this.load();
  }

  private load(): void {
    this.prescriptionService.getById(this.prescriptionId).subscribe((prescription) => {
      this.prescription.set(prescription);
      forkJoin({
        patient: this.patientService.getById(prescription.patientId),
        provider: this.providerService.getById(prescription.providerId),
      }).subscribe(({ patient, provider }) => {
        this.patient.set(patient);
        this.provider.set(provider);
      });
    });
  }

  sign(): void {
    this.actionInFlight.set(true);
    this.prescriptionService
      .sign(this.prescriptionId)
      .pipe(finalize(() => this.actionInFlight.set(false)))
      .subscribe((prescription) => {
        this.prescription.set(prescription);
        this.notifications.success('Prescription signed.');
      });
  }

  repeat(): void {
    this.actionInFlight.set(true);
    this.prescriptionService
      .repeat(this.prescriptionId)
      .pipe(finalize(() => this.actionInFlight.set(false)))
      .subscribe((repeated) => {
        this.notifications.success(`Repeat prescription ${repeated.scid} created.`);
        this.router.navigate(['/prescriptions', repeated.id]);
      });
  }
}
