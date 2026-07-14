import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { filter, finalize, forkJoin } from 'rxjs';
import { PrescriptionHubService } from '../../../core/services/prescription-hub.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { PatientService } from '../../../core/services/patient.service';
import { ProviderService } from '../../../core/services/provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Prescription } from '../../../core/models/prescription.model';
import { Patient } from '../../../core/models/patient.model';
import { Provider } from '../../../core/models/provider.model';
import { statusToastKind } from '../../../shared/models/prescription-status';
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
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly destroyRef = inject(DestroyRef);
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

  get isRejected(): boolean {
    return this.prescription()?.status === 'Rejected';
  }

  constructor() {
    this.load();

    this.prescriptionHub.statusChanged$
      .pipe(
        filter(({ prescriptionId }) => prescriptionId === this.prescriptionId),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ status }) => {
        const current = this.prescription();
        if (current) {
          this.prescription.set({ ...current, status });
        }
        this.notifications.show(`Status updated: ${status}`, statusToastKind(status));
      });
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

  /** Rejected is deliberately not repeatable (Prescription.Repeat() excludes it) - the pharmacy
   * declined this exact prescription, so re-issuing the same medications would likely hit the
   * same reason again. The correction is a fresh prescription, pre-filling only the patient
   * (same queryParams contract prescription-form already supports) so the prescriber
   * consciously re-picks provider/medications in light of why this one was rejected. */
  createReplacement(): void {
    const patientId = this.prescription()?.patientId;
    this.router.navigate(['/prescriptions/new'], { queryParams: { patientId } });
  }
}
