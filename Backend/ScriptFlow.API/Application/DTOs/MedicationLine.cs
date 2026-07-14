namespace ScriptFlow.API.Application.DTOs;

/// <summary>One medication as submitted by the client when creating/updating a prescription.
/// Route, Strength, IsPrn and Notes are optional clinical detail; the older six fields remain
/// required so existing callers (and the domain guards) are unaffected.</summary>
public sealed record MedicationLine(
    Guid MedicineId,
    string TakeValue,
    string Frequency,
    string Duration,
    int Quantity,
    string Directions,
    string? Route = null,
    string? Strength = null,
    bool IsPrn = false,
    string? Notes = null);
