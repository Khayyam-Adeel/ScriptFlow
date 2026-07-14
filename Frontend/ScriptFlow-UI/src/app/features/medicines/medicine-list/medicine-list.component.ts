import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, map, startWith, switchMap, takeUntil } from 'rxjs';
import { MedicineService } from '../../../core/services/medicine.service';
import { Medicine } from '../../../core/models/medicine.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

const SEARCH_DEBOUNCE_MS = 300;

/** Read-only medicines catalog: everything a prescription's medication lines can reference. */
@Component({
  selector: 'app-medicine-list',
  standalone: true,
  imports: [ReactiveFormsModule, SpinnerComponent, IconComponent],
  templateUrl: './medicine-list.component.html',
  styleUrl: './medicine-list.component.css',
})
export class MedicineListComponent implements OnDestroy {
  private readonly medicineService = inject(MedicineService);
  private readonly destroyed$ = new Subject<void>();

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly medicines = signal<Medicine[]>([]);
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
          return this.medicineService.list(query || undefined);
        }),
        takeUntil(this.destroyed$),
      )
      .subscribe((medicines) => {
        this.medicines.set(medicines);
        this.loading.set(false);
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
