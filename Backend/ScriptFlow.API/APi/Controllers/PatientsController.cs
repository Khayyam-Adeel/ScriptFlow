using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Finds patients by name or NHI, so a prescription can be created against one of them.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SearchPatientsQuery(query), cancellationToken);
        return Ok(result);
    }
}
