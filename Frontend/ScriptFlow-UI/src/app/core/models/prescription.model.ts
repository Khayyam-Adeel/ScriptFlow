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
  repeats: number;
  repeatsUsed: number;
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
  repeats?: number;
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
  // Only populated by list() - the grid's batch lookup. Null from getById()/create()/etc.,
  // which don't build that lookup since the detail page already fetches patient/provider
  // separately.
  patientName: string | null;
  providerName: string | null;
  canRepeatDispense: boolean;
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

// Mirrors ScriptFlow.API.Application.DTOs.LocationVolumeDto
export interface LocationVolume {
  locationName: string;
  count: number;
}

// Mirrors ScriptFlow.API.Application.DTOs.RejectionRateDto - shared shape for the by-location
// and by-provider rejection rate reports.
export interface RejectionRate {
  name: string;
  rejectedCount: number;
  finalizedCount: number;
  rejectionRatePct: number;
}
