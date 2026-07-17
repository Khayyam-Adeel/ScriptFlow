using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Queries;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetDeadLetterQueueSummaryQueryHandler
    : IRequestHandler<GetDeadLetterQueueSummaryQuery, IReadOnlyList<DeadLetterQueueSummaryDto>>
{
    private readonly IDlqRedriveService _dlqService;

    public GetDeadLetterQueueSummaryQueryHandler(IDlqRedriveService dlqService)
    {
        _dlqService = dlqService;
    }

    public async Task<IReadOnlyList<DeadLetterQueueSummaryDto>> Handle(
        GetDeadLetterQueueSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _dlqService.GetSummaryAsync(cancellationToken);
        return summary.Select(s => new DeadLetterQueueSummaryDto(s.QueueName, s.MessageCount)).ToList();
    }
}
