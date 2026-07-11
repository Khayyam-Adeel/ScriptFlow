import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';
import { PatientService } from '../../../core/services/patient.service';
import { Patient } from '../../../core/models/patient.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

const SEARCH_DEBOUNCE_MS = 300;

/** Debounced patient search so a prescriber can find a patient by name or NHI before creating a prescription. */
@Component({
  selector: 'app-patient-search',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonComponent, SpinnerComponent],
  templateUrl: './patient-search.component.html',
  styleUrl: './patient-search.component.css',
})
export class PatientSearchComponent implements OnDestroy {
  private readonly patientService = inject(PatientService);
  private readonly destroyed$ = new Subject<void>();

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly results = signal<Patient[]>([]);
  readonly loading = signal(false);
  readonly searched = signal(false);

  constructor() {
    this.queryControl.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((query) => {
          const trimmed = query.trim();
          if (!trimmed) {
            this.searched.set(false);
            this.results.set([]);
            return [];
          }

          this.loading.set(true);
          this.searched.set(true);
          return this.patientService.search(trimmed);
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe((patients) => {
        this.results.set(patients);
        this.loading.set(false);
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
