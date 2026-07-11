using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Lists practice locations, optionally filtered to one practice, for provider/prescription pickers.</summary>
public sealed record ListPracticeLocationsQuery(Guid? PracticeId) : IRequest<IReadOnlyCollection<PracticeLocationDto>>;
