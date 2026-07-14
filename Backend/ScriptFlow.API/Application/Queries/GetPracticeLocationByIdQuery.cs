using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

public sealed record GetPracticeLocationByIdQuery(Guid PracticeLocationId) : IRequest<PracticeLocationDto>;
