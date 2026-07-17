using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Drill-down from the DLQ summary table - inspects (without consuming) up to
/// <paramref name="Count"/> messages in one specific dead-letter queue, so an admin can see
/// which prescriptions/events are stuck before deciding whether to redrive them.</summary>
public sealed record PeekDeadLetterQueueMessagesQuery(string QueueName, int Count)
    : IRequest<IReadOnlyList<DeadLetterMessageDto>>;
