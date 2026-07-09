namespace ScriptFlow.API.Application.DTOs;

/// <summary>One medication as submitted by the client when creating/updating a prescription.</summary>
public sealed record MedicationLine(
    Guid MedicineId,
    string TakeValue,
    string Frequency,
    string Duration,
    int Quantity,
    string Directions);
