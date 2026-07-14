namespace ScriptFlow.API.Application.DTOs;

/// <summary>Shared shape for both the by-location and by-provider rejection rate reports.</summary>
public sealed record RejectionRateDto(string Name, int RejectedCount, int FinalizedCount, decimal RejectionRatePct);
