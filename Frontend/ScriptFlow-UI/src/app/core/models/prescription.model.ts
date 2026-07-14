import { PrescriptionStatus } from '../../shared/models/prescription-status';

// Mirrors ScriptFlow.API.Application.DTOs.MedicationDto
export interface Medication {
  id: string;
  medicineId: string;
  medicineName: string;
  takeValue: string;
  frequency: string;
  duration: string;
  quantity: number;
  directions: string;
  route: string | null;
  strength: string | null;
  isPrn: boolean;
  notes: string | null;
}

// Mirrors ScriptFlow.API.Application.DTOs.MedicationLine — one medication line submitted by the client.
export interface MedicationLine {
  medicineId: string;
  takeValue: string;
  frequency: string;
  duration: string;
  quantity: number;
  directions: string;
  route?: string | null;
  strength?: string | null;
  isPrn?: boolean;
  notes?: string | null;
}

// Mirrors ScriptFlow.API.Application.DTOs.PrescriptionDto
export interface Prescription {
  id: string;
  scid: string;
  patientId: string;
  providerId: string;
  practiceLocationId: string;
  status: PrescriptionStatus;
  repeatOfPrescriptionId: string | null;
  createdAtUtc: string;
  signedAtUtc: string | null;
  rejectionReason: string | null;
  medications: Medication[];
}

// Mirrors ScriptFlow.API.Application.Commands.CreatePrescriptionCommand
export interface CreatePrescriptionRequest {
  patientId: string;
  providerId: string;
  practiceLocationId: string;
  medications: MedicationLine[];
}

// Mirrors ScriptFlow.API.Application.Commands.UpdatePrescriptionCommand
export interface UpdatePrescriptionRequest {
  medications: MedicationLine[];
}

// Mirrors ScriptFlow.API.Application.DTOs.PrescriptionStatusCountDto
export interface PrescriptionStatusCount {
  status: PrescriptionStatus;
  count: number;
}

// Mirrors ScriptFlow.API.Application.DTOs.PrescriptionDailyVolumeDto
export interface PrescriptionDailyVolume {
  date: string;
  count: number;
}
