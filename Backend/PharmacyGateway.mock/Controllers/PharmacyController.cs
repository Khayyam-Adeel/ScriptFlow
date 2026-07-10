using Microsoft.AspNetCore.Mvc;
using Shared.contract.Contracts;
using Shared.contract.Enums;

namespace PharmacyGateway.mock.Controllers;

[ApiController]
[Route("api/pharmacy")]
public sealed class PharmacyController : ControllerBase
{
    private readonly ILogger<PharmacyController> _logger;

    public PharmacyController(ILogger<PharmacyController> logger)
    {
        _logger = logger;
    }

    // MainSpec.md requires this gateway to be "slow and unreliable": it must randomly
    // delay, ack, nack (reject), and drop requests, so that Dispatch.Worker has something
    // real to retry against and dead-letter. This endpoint is that simulation.
    [HttpPost("dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] PharmacyDispatchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received dispatch request for prescription {PrescriptionId} ({Scid}) [{CorrelationId}]",
            request.PrescriptionId, request.Scid, request.CorrelationId);

        // Step 1: always be slow. A real pharmacy gateway is a third-party system with its
        // own latency; simulate that with a random delay before answering at all.
        var delayMilliseconds = Random.Shared.Next(100, 2000);
        await Task.Delay(delayMilliseconds, cancellationToken);

        // Step 2: roll a random outcome. Weighted so most requests succeed, matching a
        // realistic gateway rather than one that fails constantly.
        var roll = Random.Shared.Next(0, 100);

        if (roll < 20)
        {
            // "Drop" - simulate a gateway that never answers by severing the connection
            // outright, rather than returning a status code. This is what should cause
            // Dispatch.Worker's HTTP client to throw and its Polly retry policy to kick in.
            _logger.LogWarning(
                "Simulating a dropped connection for prescription {PrescriptionId} after {DelayMs}ms",
                request.PrescriptionId, delayMilliseconds);
            HttpContext.Abort();
            return new EmptyResult();
        }

        if (roll < 40)
        {
            // "Nack" - a legitimate business rejection, not a failure. No retry should
            // happen for this outcome; it's a final answer from the pharmacy.
            var response = new PharmacyDispatchResponse
            {
                Outcome = PharmacyDispatchOutcome.Rejected,
                RejectionReason = "OutOfStock"
            };
            _logger.LogInformation("Rejecting prescription {PrescriptionId}: {Reason}", request.PrescriptionId, response.RejectionReason);
            return Ok(response);
        }

        // "Ack" - the common case: the pharmacy accepts the prescription.
        var acknowledged = new PharmacyDispatchResponse
        {
            Outcome = PharmacyDispatchOutcome.Acknowledged,
            PharmacyReference = Guid.NewGuid()
        };
        _logger.LogInformation(
            "Acknowledging prescription {PrescriptionId} with reference {PharmacyReference}",
            request.PrescriptionId, acknowledged.PharmacyReference);
        return Ok(acknowledged);
    }
}
