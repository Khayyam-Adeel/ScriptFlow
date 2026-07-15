import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
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
import { IconComponent, IconName } from '../../../shared/components/icon/icon.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { PrescriptionPrintComponent } from '../prescription-print/prescription-print.component';

interface LifecycleStep {
  label: string;
  state: 'done' | 'current' | 'pending' | 'failed';
}

const STEP_ICONS: Record<string, IconName> = {
  Created: 'file-text',
  Signed: 'pen',
  Dispatched: 'send',
  Acknowledged: 'check-circle',
  Rejected: 'x',
  Expired: 'clock',
};

const STEP_CAPTIONS: Record<LifecycleStep['state'], string> = {
  done: 'Completed',
  current: 'In progress',
  pending: 'Waiting',
  failed: 'Confirmed',
};

// Only positive progress gets a little celebration - a rejection/expiry is a real outcome a
// prescriber needs to act on, not something to throw confetti at.
const CELEBRATED_STATUSES = new Set(['Signed', 'Dispatched', 'Acknowledged']);
const FIREWORK_DURATION_MS = 900;

/**
 * Sign/Repeat/Edit availability mirrors Prescription.cs's state machine exactly, so this UI
 * never offers an action the API would reject with 409 Conflict.
 */
@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    ButtonComponent,
    StatusBadgeComponent,
    SpinnerComponent,
    IconComponent,
    ModalComponent,
    PrescriptionPrintComponent,
  ],
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

  readonly prescriptionId = this.route.snapshot.paramMap.get('id')!;

  readonly prescription = signal<Prescription | null>(null);
  readonly patient = signal<Patient | null>(null);
  readonly provider = signal<Provider | null>(null);
  readonly actionInFlight = signal(false);
  readonly printPreviewOpen = signal(false);

  /** Set to a step's label for a brief moment when that step is freshly reached, so its icon
   * tile can play a little firework burst - cleared automatically after FIREWORK_DURATION_MS. */
  readonly burstStep = signal<string | null>(null);
  // Destinations for the little firework's sparks - a single shared CSS rule reads these via
  // --tx/--ty custom properties instead of eight near-duplicate nth-child blocks.
  readonly fireworkSparks: ReadonlyArray<{ tx: number; ty: number; color: string }> = [
    { tx: 0, ty: -28, color: '#f43f5e' },
    { tx: 20, ty: -20, color: '#fbbf24' },
    { tx: 28, ty: 0, color: '#22c55e' },
    { tx: 20, ty: 20, color: '#3b82f6' },
    { tx: 0, ty: 28, color: '#a855f7' },
    { tx: -20, ty: 20, color: '#f43f5e' },
    { tx: -28, ty: 0, color: '#fbbf24' },
    { tx: -20, ty: -20, color: '#22c55e' },
  ];
  private burstTimeout?: ReturnType<typeof setTimeout>;

  get canSign(): boolean {
    return this.prescription()?.status === 'Created';
  }

  get canEdit(): boolean {
    return this.prescription()?.status === 'Created';
  }

  get canRepeat(): boolean {
    return this.prescription()?.canRepeatDispense ?? false;
  }

  get hasAnyRepeats(): boolean {
    return (this.prescription()?.medications ?? []).some((m) => m.repeats > 0);
  }

  get isRejected(): boolean {
    return this.prescription()?.status === 'Rejected';
  }

  /** Only offered while still Dispatched - once it moves to Acknowledged/Rejected/Expired the
   * pharmacy has already answered (or it's been swept up as stale) and there's nothing to retry. */
  get canRedispatch(): boolean {
    return this.prescription()?.status === 'Dispatched';
  }

  /** A persistent, always-visible view of Created -> Signed -> Dispatched -> outcome, so the
   * full lifecycle is legible regardless of how quickly (or slowly) the async Dispatch.Worker
   * -> pharmacy -> SignalR round trip lands - a toast alone can flash by in under a second.
   * Signed/Dispatched are derived from persisted facts (signedAtUtc, and Dispatched/
   * Acknowledged/Rejected all require having passed through Dispatched per the state machine's
   * own EnsureStatus checks), not just "current status", so a prescription that expired before
   * ever being signed correctly shows Signed as never-reached rather than skipped-over. */
  readonly lifecycleSteps = computed<LifecycleStep[]>(() => {
    const p = this.prescription();
    if (!p) {
      return [];
    }

    const status = p.status;
    const signed = p.signedAtUtc !== null;
    const dispatched = status === 'Dispatched' || status === 'Acknowledged' || status === 'Rejected';
    const isTerminal = status === 'Acknowledged' || status === 'Rejected' || status === 'Expired';

    const outcomeLabel = status === 'Rejected' ? 'Rejected' : status === 'Expired' ? 'Expired' : 'Acknowledged';
    const outcomeState: LifecycleStep['state'] =
      status === 'Acknowledged' ? 'done' : status === 'Rejected' || status === 'Expired' ? 'failed' : 'pending';

    return [
      { label: 'Created', state: 'done' },
      { label: 'Signed', state: signed ? 'done' : status === 'Created' ? 'current' : 'pending' },
      { label: 'Dispatched', state: dispatched ? 'done' : signed ? 'current' : 'pending' },
      { label: outcomeLabel, state: isTerminal ? outcomeState : dispatched ? 'current' : 'pending' },
    ];
  });

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
          // Optimistic merge first, so the lifecycle cards animate immediately rather than
          // waiting on a round trip; followed by a full refetch, since fields the backend
          // computes from more than just status (e.g. medications' repeatsUsed and
          // canRepeatDispense after an Acknowledge) would otherwise go stale.
          this.prescription.set({ ...current, status });
          this.prescriptionService.getById(this.prescriptionId).subscribe((refreshed) => this.prescription.set(refreshed));
        }
        this.notifications.show(`Status updated: ${status}`, statusToastKind(status));

        if (CELEBRATED_STATUSES.has(status)) {
          this.celebrate(status);
        }
      });
  }

  private celebrate(stepLabel: string): void {
    clearTimeout(this.burstTimeout);
    // Reset to null first so re-triggering the same step (rare, but possible on a reconnect
    // replay) restarts the CSS animation instead of a no-op class toggle being ignored.
    this.burstStep.set(null);
    requestAnimationFrame(() => {
      this.burstStep.set(stepLabel);
      this.burstTimeout = setTimeout(() => this.burstStep.set(null), FIREWORK_DURATION_MS);
    });
  }

  stepIcon(label: string): IconName {
    return STEP_ICONS[label] ?? 'file-text';
  }

  stepCaption(state: LifecycleStep['state']): string {
    return STEP_CAPTIONS[state];
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

  /** A repeat dispense re-enters the same prescription into the Sign -> Dispatch -> Acknowledge
   * pipeline (see RequestRepeatDispenseCommandHandler) - it's the same SCID and signature, so
   * there's nowhere new to navigate to; just refresh this page's state and let the lifecycle
   * cards/SignalR updates show it moving through Dispatched again. */
  repeat(): void {
    this.actionInFlight.set(true);
    this.prescriptionService
      .repeat(this.prescriptionId)
      .pipe(finalize(() => this.actionInFlight.set(false)))
      .subscribe((updated) => {
        this.prescription.set(updated);
        this.notifications.success('Repeat dispense requested.');
      });
  }

  /** Only meaningful while genuinely stuck (the pharmacy call never got an answer, e.g. it
   * permanently failed after Dispatch.Worker's own retries) - confirmed first since clicking it
   * while the original attempt is still legitimately in flight would send the prescription to
   * the pharmacy a second time. */
  redispatch(): void {
    if (!confirm('Resend this prescription to the pharmacy? Only do this if you\'re sure the original attempt failed.')) {
      return;
    }

    this.actionInFlight.set(true);
    this.prescriptionService
      .redispatch(this.prescriptionId)
      .pipe(finalize(() => this.actionInFlight.set(false)))
      .subscribe((updated) => {
        this.prescription.set(updated);
        this.notifications.success('Prescription resent to the pharmacy.');
      });
  }

  /** Rejected is deliberately not repeatable - the pharmacy declined this exact prescription, so
   * re-issuing the same medications would likely hit the same reason again. The correction is a
   * fresh prescription, pre-filling only the patient (same queryParams contract
   * prescription-form already supports) so the prescriber consciously re-picks
   * provider/medications in light of why this one was rejected. */
  createReplacement(): void {
    const patientId = this.prescription()?.patientId;
    this.router.navigate(['/prescriptions/new'], { queryParams: { patientId } });
  }

  openPrintPreview(): void {
    this.printPreviewOpen.set(true);
  }

  closePrintPreview(): void {
    this.printPreviewOpen.set(false);
  }
}
