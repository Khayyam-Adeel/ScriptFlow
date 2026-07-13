using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;
using Shared.contract.Enums;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public sealed class PrescriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrescriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>FR-001: doctor creates a prescription. FR-003 patient validation happens in the handler.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Only allowed while the prescription is still in Created status.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrescriptionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { PrescriptionId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>FR-002: doctor signs the prescription, publishing PrescriptionSignedEvent.</summary>
    [HttpPost("{id:guid}/sign")]
    public async Task<IActionResult> Sign(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SignPrescriptionCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/repeat")]
    public async Task<IActionResult> Repeat(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RepeatPrescriptionCommand(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPrescriptionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>For the dashboard's status tiles - counts across every prescription, not a page
    /// of them (unlike List, which is capped). "status-counts" is a fixed literal, so it never
    /// collides with GetById's {id:guid} route.</summary>
    [HttpGet("status-counts")]
    public async Task<IActionResult> GetStatusCounts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPrescriptionStatusCountsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patientId, [FromQuery] PrescriptionStatus? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPrescriptionsQuery(patientId, status), cancellationToken);
        return Ok(result);
    }
}
