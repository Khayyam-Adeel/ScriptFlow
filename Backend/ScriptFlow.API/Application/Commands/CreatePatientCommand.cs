using MediatR;
using ScriptFlow.API.Application.DTOs;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Commands;

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    string Address,
    string Nhi,
    DateOnly DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string Email) : IRequest<PatientDto>;
