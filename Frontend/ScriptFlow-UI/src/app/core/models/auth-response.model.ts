// Mirrors ScriptFlow.API.Application.DTOs.AuthResponse
export interface AuthResponse {
  email: string;
  role: 'Prescriber' | 'Admin';
  token: string;
  expiresAtUtc: string;
}
