import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, debounceTime, exhaustMap, forkJoin, of, startWith } from 'rxjs';
import { PrescriptionHubService } from '../../core/services/prescription-hub.service';
import { PrescriptionService } from '../../core/services/prescription.service';
import { AuthService } from '../../core/services/auth.service';
import { Prescription, PrescriptionDailyVolume, PrescriptionStatusCount } from '../../core/models/prescription.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { SpinnerComponent } from '../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

const RECENT_PRESCRIPTIONS_LIMIT = 8;

/**
 * Overview of prescription volume, status mix, and recent activity. Backed by three cheap,
 * purpose-built reads rather than fetching every prescription and deriving everything
 * client-side - that stays fast even once the table holds 1M+ rows (see PERFORMANCE.md):
 *  - getStatusCounts(): a GROUP BY over the whole table (see GetPrescriptionStatusCountsQueryHandler)
 *  - getDailyVolume(): a GROUP BY bounded to the last 14 days, riding the CreatedAtUtc covering
 *    index (see GetPrescriptionDailyVolumeQueryHandler) - not a full-table scan
 *  - list(): the existing capped-to-200-most-recent endpoint, already sorted newest-first, so
 *    "recently created" is just its first few rows - no new query needed for that one
 * A SignalR push only carries a prescription's *new* status, not enough to patch any of this
 * incrementally, so each push instead triggers a debounced refetch of all three, coalescing a
 * burst of pushes into one round of requests.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, StatusBadgeComponent, SpinnerComponent, IconComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh$ = new Subject<void>();

  readonly greeting = (() => {
    const hour = new Date().getHours();
    return hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
  })();
  readonly userName = (() => {
    const email = this.authService.user()?.email ?? '';
    const namePart = email.split('@')[0].split(/[._-]+/)[0];
    return namePart ? namePart[0].toUpperCase() + namePart.slice(1) : '';
  })();

  readonly statusCounts = signal<PrescriptionStatusCount[]>([]);
  readonly dailyVolume = signal<PrescriptionDailyVolume[]>([]);
  private readonly recentSource = signal<Prescription[]>([]);
  readonly loading = signal(true);

  readonly recentPrescriptions = computed(() => this.recentSource().slice(0, RECENT_PRESCRIPTIONS_LIMIT));
  readonly totalCount = computed(() => this.statusCounts().reduce((sum, item) => sum + item.count, 0));
  readonly pendingCount = computed(() => this.statusCounts().find((item) => item.status === 'Created')?.count ?? 0);
  readonly createdToday = computed(() => {
    const volume = this.dailyVolume();
    return volume.length > 0 ? volume[volume.length - 1].count : 0;
  });
  readonly rejectedCount = computed(() => this.statusCounts().find((item) => item.status === 'Rejected')?.count ?? 0);
  readonly maxStatusCount = computed(() => Math.max(1, ...this.statusCounts().map((item) => item.count)));
  readonly maxDailyCount = computed(() => Math.max(1, ...this.dailyVolume().map((item) => item.count)));

  constructor() {
    this.refresh$
      .pipe(
        startWith(undefined),
        debounceTime(300),
        // exhaustMap, not switchMap: these queries can take longer than the 300ms debounce
        // window, so a burst of SignalR pushes could otherwise cancel the in-flight fetch over
        // and over via switchMap and it would never complete. exhaustMap ignores pushes that
        // land while a fetch is still pending instead of cancelling it.
        exhaustMap(() =>
          forkJoin({
            statusCounts: this.prescriptionService.getStatusCounts().pipe(catchError(() => of<PrescriptionStatusCount[]>([]))),
            dailyVolume: this.prescriptionService.getDailyVolume().pipe(catchError(() => of<PrescriptionDailyVolume[]>([]))),
            recent: this.prescriptionService.list().pipe(catchError(() => of<Prescription[]>([]))),
          }).pipe(
            // A failed fetch (401, a slow query timing out, a backend blip) must not kill this
            // subscription - an error here is terminal for the whole chain otherwise, silently
            // ending all future refreshes (including ones triggered by the SignalR push below)
            // until the page is reloaded. Each inner call already swallows its own error above,
            // so this is just a last-resort guard.
            catchError(() => EMPTY),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ statusCounts, dailyVolume, recent }) => {
        this.statusCounts.set(statusCounts);
        this.dailyVolume.set(dailyVolume);
        this.recentSource.set(recent);
        this.loading.set(false);
      });

    this.prescriptionHub.statusChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh$.next());
  }
}
