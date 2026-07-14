import { Component, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { PracticeService } from '../../../core/services/practice.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { ProviderService } from '../../../core/services/provider.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { LocationVolume, RejectionRate } from '../../../core/models/prescription.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

/** Admin console landing page: organisation-wide KPI tiles plus three charts built from the
 * "Performance chapter" reporting queries (see Performance/03_ReportingQueries.sql) that were
 * designed but never exposed as an endpoint until now. Same hand-rolled bar-chart style as
 * dashboard.component (no chart library) - a ranked bar chart of one measure per named entity
 * is a single-hue magnitude encoding, not a categorical palette, so every bar shares
 * --color-primary; the rejection-rate charts additionally thread a status colour (amber/red)
 * through past a threshold, always paired with the visible percentage label. */
@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [SpinnerComponent, IconComponent],
  templateUrl: './admin-overview.component.html',
  styleUrl: './admin-overview.component.css',
})
export class AdminOverviewComponent {
  private readonly practiceService = inject(PracticeService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly providerService = inject(ProviderService);
  private readonly prescriptionService = inject(PrescriptionService);

  readonly loading = signal(true);
  readonly practiceCount = signal(0);
  readonly locationCount = signal(0);
  readonly providerCount = signal(0);
  readonly totalPrescriptions = signal(0);
  readonly pendingPrescriptions = signal(0);

  readonly volumeByLocation = signal<LocationVolume[]>([]);
  readonly rejectionByLocation = signal<RejectionRate[]>([]);
  readonly rejectionByProvider = signal<RejectionRate[]>([]);

  readonly maxVolume = computed(() => Math.max(1, ...this.volumeByLocation().map((v) => v.count)));

  constructor() {
    forkJoin({
      practices: this.practiceService.list(),
      locations: this.practiceLocationService.list(),
      providers: this.providerService.list(),
      statusCounts: this.prescriptionService.getStatusCounts(),
      volumeByLocation: this.prescriptionService.getVolumeByLocation(),
      rejectionByLocation: this.prescriptionService.getRejectionRateByLocation(),
      rejectionByProvider: this.prescriptionService.getRejectionRateByProvider(),
    }).subscribe(
      ({
        practices,
        locations,
        providers,
        statusCounts,
        volumeByLocation,
        rejectionByLocation,
        rejectionByProvider,
      }) => {
        this.practiceCount.set(practices.length);
        this.locationCount.set(locations.length);
        this.providerCount.set(providers.length);
        this.totalPrescriptions.set(statusCounts.reduce((sum, item) => sum + item.count, 0));
        this.pendingPrescriptions.set(statusCounts.find((item) => item.status === 'Created')?.count ?? 0);
        this.volumeByLocation.set(volumeByLocation);
        this.rejectionByLocation.set(rejectionByLocation);
        this.rejectionByProvider.set(rejectionByProvider);
        this.loading.set(false);
      },
    );
  }

  /** Status colour threshold for a rejection-rate bar - always paired with the visible
   * percentage label, so colour is never the only signal. */
  rateClass(pct: number): string {
    if (pct >= 20) {
      return 'rate-high';
    }
    if (pct >= 10) {
      return 'rate-medium';
    }
    return 'rate-low';
  }
}
