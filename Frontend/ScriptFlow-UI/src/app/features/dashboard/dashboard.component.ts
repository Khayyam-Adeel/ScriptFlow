import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, startWith, switchMap } from 'rxjs';
import { PrescriptionHubService } from '../../core/services/prescription-hub.service';
import { PrescriptionService } from '../../core/services/prescription.service';
import { PrescriptionStatusCount } from '../../core/models/prescription.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { SpinnerComponent } from '../../shared/components/spinner/spinner.component';

/**
 * Snapshot overview of prescription volume by status. Backed by a dedicated counts endpoint
 * (GetPrescriptionStatusCountsQuery) rather than fetching every prescription and counting
 * client-side - that used to work when the table was small, but a real GROUP BY is the only
 * thing that stays fast once it holds 1M+ rows (see PERFORMANCE.md). A SignalR push only
 * carries a prescription's *new* status, not its previous one, so there isn't enough
 * information to patch the counts incrementally - each push instead triggers a debounced
 * refetch of the real totals, coalescing a burst of pushes into one request.
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
  private readonly refresh$ = new Subject<void>();

  readonly statusCounts = signal<PrescriptionStatusCount[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.refresh$
      .pipe(
        startWith(undefined),
        debounceTime(300),
        switchMap(() => this.prescriptionService.getStatusCounts()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((counts) => {
        this.statusCounts.set(counts);
        this.loading.set(false);
      });

    this.prescriptionHub.statusChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh$.next());
  }
}
