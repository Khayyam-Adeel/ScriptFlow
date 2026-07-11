using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Lists providers, optionally filtered to one practice location, for prescription pickers.</summary>
public sealed record ListProvidersQuery(Guid? PracticeLocationId) : IRequest<IReadOnlyCollection<ProviderDto>>;
