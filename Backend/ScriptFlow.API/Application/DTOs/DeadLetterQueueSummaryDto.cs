namespace ScriptFlow.API.Application.DTOs;

public sealed record DeadLetterQueueSummaryDto(string QueueName, int MessageCount);
