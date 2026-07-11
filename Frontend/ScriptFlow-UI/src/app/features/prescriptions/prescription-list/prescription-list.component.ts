import { DatePipe } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, startWith, switchMap, takeUntil, timer } from 'rxjs';
import { PrescriptionHubService } from '../../../core/services/prescription-hub.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { Prescription } from '../../../core/models/prescription.model';
import { PRESCRIPTION_STATUSES, PrescriptionStatus } from '../../../shared/models/prescription-status';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';

const POLL_INTERVAL_MS = 5000;

/**
 * Live status board: a SignalR push (see PrescriptionHubService) patches a row's status the
 * moment Notification.Service broadcasts it, so a prescriber normally sees the update within
 * about a second. The 5s poll stays as a reconciliation fallback for any gap while the socket
 * is reconnecting - not the primary update path anymore. Polling still pauses while the
 * browser tab is hidden to avoid wasted requests.
 */
@Component({
  selector: 'app-prescription-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, ButtonComponent, StatusBadgeComponent, SelectFieldComponent],
  templateUrl: './prescription-list.component.html',
  styleUrl: './prescription-list.component.css',
})
export class PrescriptionListComponent implements OnDestroy {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyed$ = new Subject<void>();
  private readonly restart$ = new Subject<void>();

  readonly prescriptions = signal<Prescription[]>([]);
  readonly loading = signal(true);
  // Deep-linkable from the dashboard's status tiles, e.g. /prescriptions?status=Signed.
  readonly statusControl = new FormControl<PrescriptionStatus | ''>(
    (this.route.snapshot.queryParamMap.get('status') as PrescriptionStatus | null) ?? '',
    { nonNullable: true },
  );
  readonly statusOptions: SelectOption[] = PRESCRIPTION_STATUSES.map((status) => ({
    value: status,
    label: status,
  }));

  constructor() {
    this.statusControl.valueChanges.pipe(takeUntil(this.destroyed$)).subscribe(() => {
      this.loading.set(true);
      this.restart$.next();
    });

    // restart$ cancels the in-flight poll timer (via switchMap) whenever the status filter
    // changes, so there is only ever one active 5s interval at a time.
    this.restart$
      .pipe(
        startWith(undefined),
        switchMap(() => timer(0, POLL_INTERVAL_MS)),
        switchMap(() => {
          if (document.visibilityState === 'hidden') {
            return [];
          }
          const status = this.statusControl.value || undefined;
          return this.prescriptionService.list(undefined, status);
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe({
        next: (prescriptions) => {
          this.prescriptions.set(prescriptions);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });

    this.prescriptionHub.statusChanged$.pipe(takeUntil(this.destroyed$)).subscribe(({ prescriptionId, status }) => {
      this.prescriptions.update((list) =>
        list.map((p) => (p.id === prescriptionId ? { ...p, status } : p)),
      );
    });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
