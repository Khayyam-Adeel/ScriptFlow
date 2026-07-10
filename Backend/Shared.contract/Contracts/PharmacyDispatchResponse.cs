using Shared.contract.Enums;

namespace Shared.contract.Contracts;

// What PharmacyGateway.mock replies with when it doesn't drop the connection.
public sealed class PharmacyDispatchResponse
{
    public required PharmacyDispatchOutcome Outcome { get; init; }

    // Set only when Outcome is Acknowledged.
    public Guid? PharmacyReference { get; init; }

    // Set only when Outcome is Rejected.
    public string? RejectionReason { get; init; }
}
