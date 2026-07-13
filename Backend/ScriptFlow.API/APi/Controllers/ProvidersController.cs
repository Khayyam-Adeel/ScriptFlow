using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Queries;
using Shared.contract.Enums;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registering a new provider is an administrative action - Admin only. Any
    /// authenticated user can still browse the provider list/detail below.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProviderCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProviderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Lists providers for the provider picker on the prescription form, optionally scoped to one practice location.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? practiceLocationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListProvidersQuery(practiceLocationId), cancellationToken);
        return Ok(result);
    }
}
