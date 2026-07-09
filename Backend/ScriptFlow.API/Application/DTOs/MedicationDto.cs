namespace ScriptFlow.API.Application.DTOs;

public sealed record MedicationDto(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    string TakeValue,
    string Frequency,
    string Duration,
    int Quantity,
    string Directions);
