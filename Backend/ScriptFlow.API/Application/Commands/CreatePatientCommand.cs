using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    string Address,
    string Nhi) : IRequest<PatientDto>;
