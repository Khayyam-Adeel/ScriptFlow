using Shared.contract.Enums;

namespace Shared.Events;

// Published by Dispatch.Worker as soon as it starts attempting delivery to the pharmacy
// gateway - before it knows the outcome. Distinct from PrescriptionAcknowledgedEvent/
// PrescriptionRejectedEvent, which carry the pharmacy's actual answer.
public sealed class PrescriptionDispatchedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }
    public required DateTime DispatchedAtUtc { get; init; }
    public required PrescriptionStatus Status { get; init; }
}
