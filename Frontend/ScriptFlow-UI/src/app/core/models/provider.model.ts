import { ProviderType } from '../../shared/models/provider-type';

// Mirrors ScriptFlow.API.Application.DTOs.ProviderDto
export interface Provider {
  id: string;
  firstName: string;
  lastName: string;
  type: ProviderType;
  nzmcNo: string;
  practiceLocationId: string;
  email: string;
  phoneNumber: string;
  qualification: string;
}

// Mirrors ScriptFlow.API.Application.Commands.CreateProviderCommand
export interface CreateProviderRequest {
  firstName: string;
  lastName: string;
  type: ProviderType;
  nzmcNo: string;
  practiceLocationId: string;
  email: string;
  phoneNumber: string;
  qualification: string;
}
