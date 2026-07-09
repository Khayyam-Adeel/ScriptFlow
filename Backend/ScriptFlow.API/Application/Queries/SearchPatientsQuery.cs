using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Finds patients by name or NHI so a prescription can be created against one of them.</summary>
public sealed record SearchPatientsQuery(string Query) : IRequest<IReadOnlyCollection<PatientDto>>;
