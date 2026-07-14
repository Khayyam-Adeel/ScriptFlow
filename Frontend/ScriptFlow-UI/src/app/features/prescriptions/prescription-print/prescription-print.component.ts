import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { PatientService } from '../../../core/services/patient.service';
import { ProviderService } from '../../../core/services/provider.service';
import { PracticeLocationService } from '../../../core/services/practice-location.service';
import { MedicineService } from '../../../core/services/medicine.service';
import { Prescription } from '../../../core/models/prescription.model';
import { Patient } from '../../../core/models/patient.model';
import { Provider } from '../../../core/models/provider.model';
import { PracticeLocation } from '../../../core/models/practice-location.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { BarcodeComponent } from '../../../shared/components/barcode/barcode.component';

const PROVIDER_TITLES: Record<string, string> = {
  Doctor: 'Dr',
  Nurse: 'RN',
  Student: 'Student Dr',
};

/** Print-styled view of one prescription: practice/provider/patient header, a Code128
 * barcode of the SCID, and the medication list - modelled on a real NZ prescription
 * print-out. Meant to be projected inside `app-modal`; the host page opens the modal right
 * after creating a prescription (before it's signed) and can reopen it anytime as a reprint. */
@Component({
  selector: 'app-prescription-print',
  standalone: true,
  imports: [DatePipe, ButtonComponent, IconComponent, SpinnerComponent, BarcodeComponent],
  templateUrl: './prescription-print.component.html',
  styleUrl: './prescription-print.component.css',
})
export class PrescriptionPrintComponent implements OnInit {
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly patientService = inject(PatientService);
  private readonly providerService = inject(ProviderService);
  private readonly practiceLocationService = inject(PracticeLocationService);
  private readonly medicineService = inject(MedicineService);

  @Input({ required: true }) prescriptionId!: string;
  @Output() continueClicked = new EventEmitter<void>();

  // `prescription()` being null doubles as the loading indicator - it's only set once every
  // related lookup (patient/provider/practice location/medicine forms) has also resolved, so
  // the print sheet never renders with a partially-loaded header.
  readonly prescription = signal<Prescription | null>(null);
  readonly patient = signal<Patient | null>(null);
  readonly provider = signal<Provider | null>(null);
  readonly practiceLocation = signal<PracticeLocation | null>(null);
  readonly medicineForms = signal<Map<string, string>>(new Map());

  get providerTitle(): string {
    const type = this.provider()?.type;
    return (type && PROVIDER_TITLES[type]) || '';
  }

  ngOnInit(): void {
    this.prescriptionService.getById(this.prescriptionId).subscribe((prescription) => {
      forkJoin({
        patient: this.patientService.getById(prescription.patientId),
        provider: this.providerService.getById(prescription.providerId),
        practiceLocations: this.practiceLocationService.list(),
        medicines: this.medicineService.list(),
      }).subscribe(({ patient, provider, practiceLocations, medicines }) => {
        this.patient.set(patient);
        this.provider.set(provider);
        this.practiceLocation.set(
          practiceLocations.find((location) => location.id === prescription.practiceLocationId) ?? null,
        );
        this.medicineForms.set(new Map(medicines.map((medicine) => [medicine.id, medicine.form])));
        this.prescription.set(prescription);
      });
    });
  }

  medicineForm(medicineId: string): string {
    return this.medicineForms().get(medicineId) ?? 'unit';
  }

  print(): void {
    window.print();
  }
}
