using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScriptFlow.API.Application.Queries;

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

    /// <summary>Lists practice locations for provider/prescription pickers, optionally scoped to one practice.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? practiceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPracticeLocationsQuery(practiceId), cancellationToken);
        return Ok(result);
    }
}
