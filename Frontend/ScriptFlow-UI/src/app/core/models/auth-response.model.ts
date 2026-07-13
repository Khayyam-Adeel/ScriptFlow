// Mirrors ScriptFlow.API.Application.DTOs.AuthResponse
export interface AuthResponse {
  email: string;
  role: 'Prescriber' | 'Admin';
  token: string;
  expiresAtUtc: string;
}

// Mirrors ScriptFlow.API.Application.DTOs.CreatedUserDto
export interface CreatedUser {
  id: string;
  email: string;
  role: 'Prescriber' | 'Admin';
}
