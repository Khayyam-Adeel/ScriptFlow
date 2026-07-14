import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, map, startWith, switchMap, takeUntil } from 'rxjs';
import { PatientService } from '../../../core/services/patient.service';
import { Patient } from '../../../core/models/patient.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

const SEARCH_DEBOUNCE_MS = 300;

/**
 * Lists all patients on load, with debounced name/NHI filtering. Clicking a patient starts a new
 * prescription for them (the row's "View" link opens the patient detail instead).
 */
@Component({
  selector: 'app-patient-search',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonComponent, SpinnerComponent, IconComponent],
  templateUrl: './patient-search.component.html',
  styleUrl: './patient-search.component.css',
})
export class PatientSearchComponent implements OnDestroy {
  private readonly patientService = inject(PatientService);
  private readonly destroyed$ = new Subject<void>();

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly results = signal<Patient[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.queryControl.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        map((query) => query.trim()),
        distinctUntilChanged(),
        startWith(''),
        switchMap((query) => {
          this.loading.set(true);
          return this.patientService.search(query);
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
