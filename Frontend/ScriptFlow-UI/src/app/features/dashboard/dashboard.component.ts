import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, debounceTime, exhaustMap, startWith } from 'rxjs';
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
        // exhaustMap, not switchMap: this GROUP BY runs over 1M+ rows and can take longer than
        // the 300ms debounce window, so a burst of SignalR pushes could otherwise cancel the
        // in-flight fetch over and over via switchMap and it would never complete. exhaustMap
        // ignores pushes that land while a fetch is still pending instead of cancelling it.
        exhaustMap(() =>
          this.prescriptionService.getStatusCounts().pipe(
            // A failed fetch (401, a slow query timing out, a backend blip) must not kill this
            // subscription - an error here is terminal for the whole chain otherwise, silently
            // ending all future refreshes (including ones triggered by the SignalR push below)
            // until the page is reloaded. Swallow it and keep the last known-good counts.
            catchError(() => {
              this.loading.set(false);
              return EMPTY;
            }),
          ),
        ),
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
