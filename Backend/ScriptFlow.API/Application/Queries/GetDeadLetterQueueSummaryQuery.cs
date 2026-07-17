using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>For the admin "dead-letter queues" page's overview table - message count per
/// known DLQ, so an admin can see at a glance which pipeline stage has stuck work.</summary>
public sealed record GetDeadLetterQueueSummaryQuery : IRequest<IReadOnlyList<DeadLetterQueueSummaryDto>>;
