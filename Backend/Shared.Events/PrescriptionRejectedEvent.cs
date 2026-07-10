using Shared.contract.Enums;

namespace Shared.Events;

// Published by Dispatch.Worker when the pharmacy gateway gives a business rejection
// (e.g. out of stock) - a legitimate terminal outcome, not a dispatch failure.
public sealed class PrescriptionRejectedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }
    public required string RejectionReason { get; init; }
    public required DateTime RejectedAtUtc { get; init; }
    public required PrescriptionStatus Status { get; init; }
}
