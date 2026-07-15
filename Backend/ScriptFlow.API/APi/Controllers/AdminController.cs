using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
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
