namespace ScriptFlow.API.Application.DTOs;

public sealed record RedriveDeadLetterQueueResult(string QueueName, int RedrivenCount);
