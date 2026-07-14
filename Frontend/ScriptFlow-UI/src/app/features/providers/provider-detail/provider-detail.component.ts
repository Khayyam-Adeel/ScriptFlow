import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProviderService } from '../../../core/services/provider.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { Provider } from '../../../core/models/provider.model';
import { Prescription } from '../../../core/models/prescription.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

/** Provider record: identity card plus their recent prescriptions (list() filtered by providerId). */
@Component({
  selector: 'app-provider-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, SpinnerComponent, IconComponent, StatusBadgeComponent],
  templateUrl: './provider-detail.component.html',
  styleUrl: './provider-detail.component.css',
})
export class ProviderDetailComponent {
  private readonly providerService = inject(ProviderService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly route = inject(ActivatedRoute);

  readonly provider = signal<Provider | null>(null);
  readonly locationName = signal<string>('');
  readonly prescriptions = signal<Prescription[]>([]);
  readonly prescriptionsLoading = signal(true);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.providerService.getById(id).subscribe((provider) => {
      this.provider.set(provider);
      this.practiceLocationService.list().subscribe((locations) => {
        this.locationName.set(locations.find((l) => l.id === provider.practiceLocationId)?.name ?? '—');
      });
    });
    this.prescriptionService.list({ providerId: id }).subscribe((prescriptions) => {
      this.prescriptions.set(prescriptions);
      this.prescriptionsLoading.set(false);
    });
  }
}
