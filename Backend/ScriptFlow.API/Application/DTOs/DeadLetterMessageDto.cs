namespace ScriptFlow.API.Application.DTOs;

/// <summary>
/// PrescriptionId/Scid are nullable because not every dead-lettered event is prescription-shaped
/// (e.g. TokenRevokedEvent) - see MessageDeadLetteredEvent's own comment for why.
/// </summary>
public sealed record DeadLetterMessageDto(
    string? MessageId,
    string EventType,
    Guid? PrescriptionId,
    string? Scid,
    string? FailureReason,
    DateTime? FailedAtUtc,
    string PayloadJson);
