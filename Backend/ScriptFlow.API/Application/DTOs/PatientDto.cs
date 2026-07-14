using Shared.contract.Enums;

namespace ScriptFlow.API.Application.DTOs;

public sealed record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Address,
    string Nhi,
    DateOnly DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string Email);
