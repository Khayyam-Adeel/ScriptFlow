// Mirrors ScriptFlow.API.Application.DTOs.DeadLetterQueueSummaryDto
export interface DeadLetterQueueSummary {
  queueName: string;
  messageCount: number;
}

// Mirrors ScriptFlow.API.Application.DTOs.DeadLetterMessageDto
export interface DeadLetterMessage {
  messageId: string | null;
  eventType: string;
  prescriptionId: string | null;
  scid: string | null;
  failureReason: string | null;
  failedAtUtc: string | null;
  payloadJson: string;
}

// Mirrors ScriptFlow.API.Application.DTOs.RedriveDeadLetterQueueResult
export interface RedriveDeadLetterQueueResult {
  queueName: string;
  redrivenCount: number;
}
