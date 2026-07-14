using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;
using Shared.contract.Enums;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/practice-locations")]
public sealed class PracticeLocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PracticeLocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registering a new practice location is an administrative action - Admin only.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePracticeLocationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPracticeLocationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Lists practice locations for provider/prescription pickers, optionally scoped to one practice.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? practiceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPracticeLocationsQuery(practiceId), cancellationToken);
        return Ok(result);
    }
}
