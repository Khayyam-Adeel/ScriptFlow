using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/medicines")]
public sealed class MedicinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicinesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lists medicines for the medication-line picker on the prescription form.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListMedicinesQuery(search), cancellationToken);
        return Ok(result);
    }
}
