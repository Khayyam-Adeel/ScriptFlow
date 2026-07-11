import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProviderService } from '../../../core/services/provider.service';
import { Provider } from '../../../core/models/provider.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

@Component({
  selector: 'app-provider-detail',
  standalone: true,
  imports: [RouterLink, SpinnerComponent],
  templateUrl: './provider-detail.component.html',
  styleUrl: './provider-detail.component.css',
})
export class ProviderDetailComponent {
  private readonly providerService = inject(ProviderService);
  private readonly route = inject(ActivatedRoute);

  readonly provider = signal<Provider | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.providerService.getById(id).subscribe((provider) => this.provider.set(provider));
  }
}
