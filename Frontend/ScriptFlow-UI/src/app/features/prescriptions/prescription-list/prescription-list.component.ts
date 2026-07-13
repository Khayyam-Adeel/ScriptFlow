import { DatePipe } from '@angular/common';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  EMPTY,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  exhaustMap,
  merge,
  startWith,
  switchMap,
  takeUntil,
  timer,
} from 'rxjs';
import { PrescriptionHubService } from '../../../core/services/prescription-hub.service';
import { PrescriptionListFilters, PrescriptionService } from '../../../core/services/prescription.service';
import { PatientService } from '../../../core/services/patient.service';
import { ProviderService } from '../../../core/services/provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Prescription } from '../../../core/models/prescription.model';
import { Patient } from '../../../core/models/patient.model';
import { Provider } from '../../../core/models/provider.model';
import { PRESCRIPTION_STATUSES, PrescriptionStatus, statusToastKind } from '../../../shared/models/prescription-status';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SelectFieldComponent, SelectOption } from '../../../shared/components/select-field/select-field.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';

const POLL_INTERVAL_MS = 5000;
const FILTER_DEBOUNCE_MS = 300;
const PAGE_SIZE = 15;

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
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    ButtonComponent,
    StatusBadgeComponent,
    SelectFieldComponent,
    TextFieldComponent,
  ],
  templateUrl: './prescription-list.component.html',
  styleUrl: './prescription-list.component.css',
})
export class PrescriptionListComponent implements OnDestroy {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly patientService = inject(PatientService);
  private readonly providerService = inject(ProviderService);
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyed$ = new Subject<void>();
  private readonly restart$ = new Subject<void>();

  readonly prescriptions = signal<Prescription[]>([]);
  readonly loading = signal(true);

  // Client-side over the already-capped (TOP 200, see usp_Prescription_List) result set - the
  // API never returns more than that in one response, so there's no need for a server round trip
  // per page.
  readonly currentPage = signal(1);
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.prescriptions().length / PAGE_SIZE)));
  readonly pagedPrescriptions = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.prescriptions().slice(start, start + PAGE_SIZE);
  });
  // Deep-linkable from the dashboard's status tiles, e.g. /prescriptions?status=Signed.
  readonly statusControl = new FormControl<PrescriptionStatus | ''>(
    (this.route.snapshot.queryParamMap.get('status') as PrescriptionStatus | null) ?? '',
    { nonNullable: true },
  );
  readonly statusOptions: SelectOption[] = PRESCRIPTION_STATUSES.map((status) => ({
    value: status,
    label: status,
  }));

  readonly scidControl = new FormControl('', { nonNullable: true });
  readonly createdFromControl = new FormControl('', { nonNullable: true });
  readonly createdToControl = new FormControl('', { nonNullable: true });

  readonly patientQuery = new FormControl('', { nonNullable: true });
  readonly patientResults = signal<Patient[]>([]);
  readonly selectedPatient = signal<Patient | null>(null);

  readonly providerQuery = new FormControl('', { nonNullable: true });
  private readonly allProviders = signal<Provider[]>([]);
  private readonly providerQueryText = signal('');
  readonly selectedProvider = signal<Provider | null>(null);
  readonly providerResults = computed(() => {
    const query = this.providerQueryText().trim().toLowerCase();
    if (!query) {
      return [];
    }
    return this.allProviders().filter((provider) =>
      `${provider.firstName} ${provider.lastName}`.toLowerCase().includes(query),
    );
  });

  constructor() {
    this.providerService.list().subscribe((providers) => this.allProviders.set(providers));

    // providerResults is a computed() signal, which only reacts to signal reads - not to a
    // FormControl's own value changes - so the query text has to be mirrored into a signal here
    // for the computed filter to actually recompute as the user types.
    this.providerQuery.valueChanges.pipe(takeUntil(this.destroyed$)).subscribe((query) => {
      this.providerQueryText.set(query);
    });

    this.patientQuery.valueChanges
      .pipe(
        debounceTime(FILTER_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((query) => {
          const trimmed = query.trim();
          return trimmed ? this.patientService.search(trimmed) : [];
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe((patients) => this.patientResults.set(patients));

    // restart$ cancels the in-flight poll timer (via switchMap) whenever any filter changes, so
    // there is only ever one active 5s interval at a time. Immediate filters (status, patient/
    // provider selection) restart right away; free-text/date filters are debounced first so a
    // few keystrokes/date edits don't each fire their own request against a 1M+ row table.
    merge(
      this.statusControl.valueChanges,
      this.scidControl.valueChanges.pipe(debounceTime(FILTER_DEBOUNCE_MS), distinctUntilChanged()),
      this.createdFromControl.valueChanges.pipe(debounceTime(FILTER_DEBOUNCE_MS), distinctUntilChanged()),
      this.createdToControl.valueChanges.pipe(debounceTime(FILTER_DEBOUNCE_MS), distinctUntilChanged()),
    )
      .pipe(takeUntil(this.destroyed$))
      .subscribe(() => this.refresh());

    this.restart$
      .pipe(
        startWith(undefined),
        switchMap(() => timer(0, POLL_INTERVAL_MS)),
        // exhaustMap, not switchMap: a slow query (this table holds 1M+ rows) can easily take
        // longer than the 5s poll interval. switchMap would cancel that in-flight request the
        // moment the next tick fires - forever, if the query is consistently slower than the
        // poll - so the list would never render. exhaustMap instead ignores ticks that land
        // while a request is still pending, letting the current one actually finish.
        exhaustMap(() => {
          if (document.visibilityState === 'hidden') {
            return EMPTY;
          }
          return this.prescriptionService.list(this.currentFilters()).pipe(
            // A failed poll tick (401, a slow query timing out, a backend blip) must not kill
            // this subscription - an error here is terminal for the whole chain otherwise,
            // silently ending every future poll tick and any reaction to the filters
            // until the page is reloaded. Swallow it; the next 5s tick retries on its own.
            catchError(() => {
              this.loading.set(false);
              return EMPTY;
            }),
          );
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe((prescriptions) => {
        this.prescriptions.set(prescriptions);
        this.loading.set(false);
        // A poll tick (or a filter change that shrinks the result set) can leave currentPage
        // past the new last page - clamp instead of showing an empty page with working Prev.
        const lastPage = Math.max(1, Math.ceil(prescriptions.length / PAGE_SIZE));
        if (this.currentPage() > lastPage) {
          this.currentPage.set(lastPage);
        }
      });

    this.prescriptionHub.statusChanged$.pipe(takeUntil(this.destroyed$)).subscribe(({ prescriptionId, status }) => {
      const matched = this.prescriptions().find((p) => p.id === prescriptionId);
      if (!matched) {
        return;
      }
      this.prescriptions.update((list) =>
        list.map((p) => (p.id === prescriptionId ? { ...p, status } : p)),
      );
      this.notifications.show(`${matched.scid}: ${status}`, statusToastKind(status));
    });
  }

  selectPatient(patient: Patient): void {
    this.selectedPatient.set(patient);
    this.patientQuery.setValue('');
    this.patientResults.set([]);
    this.refresh();
  }

  clearSelectedPatient(): void {
    this.selectedPatient.set(null);
    this.refresh();
  }

  selectProvider(provider: Provider): void {
    this.selectedProvider.set(provider);
    this.providerQuery.setValue('');
    this.refresh();
  }

  clearSelectedProvider(): void {
    this.selectedProvider.set(null);
    this.refresh();
  }

  clearFilters(): void {
    this.statusControl.setValue('', { emitEvent: false });
    this.scidControl.setValue('', { emitEvent: false });
    this.createdFromControl.setValue('', { emitEvent: false });
    this.createdToControl.setValue('', { emitEvent: false });
    this.patientQuery.setValue('', { emitEvent: false });
    this.providerQuery.setValue('', { emitEvent: false });
    this.providerQueryText.set('');
    this.patientResults.set([]);
    this.selectedPatient.set(null);
    this.selectedProvider.set(null);
    this.refresh();
  }

  private currentFilters(): PrescriptionListFilters {
    return {
      status: this.statusControl.value || undefined,
      scid: this.scidControl.value.trim() || undefined,
      createdFrom: this.createdFromControl.value || undefined,
      createdTo: this.createdToControl.value || undefined,
      patientId: this.selectedPatient()?.id,
      providerId: this.selectedProvider()?.id,
    };
  }

  goToPage(page: number): void {
    this.currentPage.set(Math.min(Math.max(1, page), this.pageCount()));
  }

  previousPage(): void {
    this.goToPage(this.currentPage() - 1);
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  private refresh(): void {
    this.loading.set(true);
    this.currentPage.set(1);
    this.restart$.next();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
