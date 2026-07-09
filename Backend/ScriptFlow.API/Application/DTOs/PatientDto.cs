namespace ScriptFlow.API.Application.DTOs;

public sealed record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Address,
    string Nhi);
