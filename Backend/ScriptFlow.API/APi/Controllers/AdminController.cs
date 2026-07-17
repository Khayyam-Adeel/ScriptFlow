using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;
using Shared.contract.Enums;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Message count for every known dead-letter queue - the overview an admin's "dead-letter
    /// queues" page lists before drilling into any one queue.
    /// </summary>
    [HttpGet("dlq")]
    public async Task<IActionResult> GetDeadLetterQueues(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDeadLetterQueueSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Non-destructively inspects up to <paramref name="count"/> messages in one dead-letter
    /// queue - each message is requeued after reading, so this never consumes what a later
    /// redrive would process.
    /// </summary>
    [HttpGet("dlq/{queueName}/messages")]
    public async Task<IActionResult> PeekDeadLetterQueueMessages(
        string queueName, [FromQuery] int count = 50, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new PeekDeadLetterQueueMessagesQuery(queueName, count), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Drains every message currently on the named dead-letter queue and republishes it to the
    /// main exchange, so whichever consumer originally owned it gets another attempt. Only
    /// queues this system actually declares are accepted - see
    /// RedriveDeadLetterQueueCommandValidator for the allow-list.
    /// </summary>
    [HttpPost("dlq/{queueName}/redrive")]
    public async Task<IActionResult> RedriveDeadLetterQueue(string queueName, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RedriveDeadLetterQueueCommand(queueName), cancellationToken);
        return Ok(result);
    }
}
