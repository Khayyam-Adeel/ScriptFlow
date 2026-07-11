import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/services/patient.service';
import { Patient } from '../../../core/models/patient.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [RouterLink, SpinnerComponent, ButtonComponent],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.css',
})
export class PatientDetailComponent {
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);

  readonly patient = signal<Patient | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.patientService.getById(id).subscribe((patient) => this.patient.set(patient));
  }
}
