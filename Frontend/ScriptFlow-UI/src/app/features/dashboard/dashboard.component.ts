import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PrescriptionHubService } from '../../core/services/prescription-hub.service';
import { PrescriptionService } from '../../core/services/prescription.service';
import { Prescription } from '../../core/models/prescription.model';
import { PRESCRIPTION_STATUSES, PrescriptionStatus } from '../../shared/models/prescription-status';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { SpinnerComponent } from '../../shared/components/spinner/spinner.component';

interface StatusCount {
  status: PrescriptionStatus;
  count: number;
}

/**
 * Snapshot overview of prescription volume by status. Fetched once on load, then patched in
 * place by SignalR status-change pushes (see PrescriptionHubService) so the counts stay live
 * without a manual refresh; the filterable, polling board lives at /prescriptions.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, SpinnerComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly prescriptions = signal<Prescription[] | null>(null);
  readonly loading = computed(() => this.prescriptions() === null);

  readonly statusCounts = computed<StatusCount[]>(() => {
    const prescriptions = this.prescriptions() ?? [];
    return PRESCRIPTION_STATUSES.map((status) => ({
      status,
      count: prescriptions.filter((p) => p.status === status).length,
    }));
  });

  constructor() {
    this.prescriptionService.list().subscribe((prescriptions) => this.prescriptions.set(prescriptions));

    this.prescriptionHub.statusChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(({ prescriptionId, status }) => {
      this.prescriptions.update((list) =>
        list ? list.map((p) => (p.id === prescriptionId ? { ...p, status } : p)) : list,
      );
    });
  }
}
