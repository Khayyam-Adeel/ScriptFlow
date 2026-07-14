// Mirrors ScriptFlow.API.Application.DTOs.PracticeLocationDto
export interface PracticeLocation {
  id: string;
  practiceId: string;
  name: string;
  hpiNumber: string;
  address: string;
  phone: string;
}

// Mirrors ScriptFlow.API.Application.Commands.CreatePracticeLocationCommand
export interface CreatePracticeLocationRequest {
  practiceId: string;
  name: string;
  hpiNo: string;
  hpiExtension: string;
  address: string;
  phone: string;
}
