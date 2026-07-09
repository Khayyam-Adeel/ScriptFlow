using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

public sealed record GetPatientByIdQuery(Guid PatientId) : IRequest<PatientDto>;
