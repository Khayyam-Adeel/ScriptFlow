namespace ScriptFlow.API.Application.DTOs;

public sealed record MedicineDto(
    Guid Id,
    string Name,
    string Sctid,
    string Form);
