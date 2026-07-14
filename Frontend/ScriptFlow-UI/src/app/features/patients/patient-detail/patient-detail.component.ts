import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/services/patient.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { Patient } from '../../../core/models/patient.model';
import { Prescription } from '../../../core/models/prescription.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

/** Patient record: identity card plus their prescription history (list() filtered by patientId). */
@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, SpinnerComponent, ButtonComponent, IconComponent, StatusBadgeComponent],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.css',
})
export class PatientDetailComponent {
  private readonly patientService = inject(PatientService);
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly route = inject(ActivatedRoute);

  readonly patient = signal<Patient | null>(null);
  readonly prescriptions = signal<Prescription[]>([]);
  readonly prescriptionsLoading = signal(true);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.patientService.getById(id).subscribe((patient) => this.patient.set(patient));
    this.prescriptionService.list({ patientId: id }).subscribe((prescriptions) => {
      this.prescriptions.set(prescriptions);
      this.prescriptionsLoading.set(false);
    });
  }
}
