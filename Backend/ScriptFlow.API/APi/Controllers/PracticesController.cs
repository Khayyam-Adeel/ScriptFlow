using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;
using Shared.contract.Enums;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/practices")]
public sealed class PracticesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PracticesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registering a new practice is an administrative action - Admin only.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePracticeCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPracticeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Lists practices for admin/provider-creation pickers.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPracticesQuery(), cancellationToken);
        return Ok(result);
    }
}
