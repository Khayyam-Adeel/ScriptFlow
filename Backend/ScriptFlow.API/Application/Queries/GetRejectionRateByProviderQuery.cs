using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

public sealed record GetRejectionRateByProviderQuery() : IRequest<IReadOnlyCollection<RejectionRateDto>>;
