using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

public sealed record GetRejectionRateByLocationQuery() : IRequest<IReadOnlyCollection<RejectionRateDto>>;
