import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { ProviderService } from '../../../core/services/provider.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { AuthService } from '../../../core/services/auth.service';
import { Provider } from '../../../core/models/provider.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

/** Directory of every prescriber in the practice, filterable by name client-side (the full
 * provider list is small reference data, unlike patients). */
@Component({
  selector: 'app-provider-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonComponent, SpinnerComponent, IconComponent],
  templateUrl: './provider-list.component.html',
  styleUrl: './provider-list.component.css',
})
export class ProviderListComponent {
  private readonly providerService = inject(ProviderService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly authService = inject(AuthService);

  readonly isAdmin = this.authService.isAdmin;
  readonly queryControl = new FormControl('', { nonNullable: true });
  private readonly query = toSignal(this.queryControl.valueChanges, { initialValue: '' });

  readonly providers = signal<Provider[]>([]);
  readonly locationNames = signal<Map<string, string>>(new Map());
  readonly loading = signal(true);

  readonly filteredProviders = computed(() => {
    const query = this.query().trim().toLowerCase();
    if (!query) {
      return this.providers();
    }
    return this.providers().filter((provider) =>
      `${provider.firstName} ${provider.lastName} ${provider.nzmcNo}`.toLowerCase().includes(query),
    );
  });

  constructor() {
    forkJoin({
      providers: this.providerService.list(),
      locations: this.practiceLocationService.list(),
    }).subscribe(({ providers, locations }) => {
      this.providers.set(providers);
      this.locationNames.set(new Map(locations.map((l) => [l.id, l.name])));
      this.loading.set(false);
    });
  }

  locationName(provider: Provider): string {
    return this.locationNames().get(provider.practiceLocationId) ?? '—';
  }
}
