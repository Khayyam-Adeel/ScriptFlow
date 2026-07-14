// Mirrors ScriptFlow.API.Application.DTOs.PracticeDto
export interface Practice {
  id: string;
  name: string;
}

// Mirrors ScriptFlow.API.Application.Commands.CreatePracticeCommand
export interface CreatePracticeRequest {
  name: string;
}
