using Shared.contract.Enums;

namespace Shared.Events;

// Published by Dispatch.Worker once the pharmacy gateway has accepted a prescription.
public sealed class PrescriptionAcknowledgedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }
    public required Guid PharmacyReference { get; init; }
    public required DateTime AcknowledgedAtUtc { get; init; }
    public required PrescriptionStatus Status { get; init; }
}
