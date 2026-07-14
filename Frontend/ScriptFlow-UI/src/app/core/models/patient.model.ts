import { Gender } from '../../shared/models/gender';

// Mirrors ScriptFlow.API.Application.DTOs.PatientDto
export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  address: string;
  nhi: string;
  dateOfBirth: string;
  gender: Gender;
  phoneNumber: string;
  email: string;
}

// Mirrors ScriptFlow.API.Application.Commands.CreatePatientCommand
export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  address: string;
  nhi: string;
  dateOfBirth: string;
  gender: Gender;
  phoneNumber: string;
  email: string;
}
