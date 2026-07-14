using Shared.contract.Enums;

namespace Shared.Events;

public sealed class PrescriptionSignedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }
    public required Guid ProviderId { get; init; }
    public required DateTime SignedAtUtc { get; init; }
    public required PrescriptionStatus Status { get; init; }

    /// <summary>True when this Signed event re-enters the pipeline for a repeat dispense of an
    /// already-signed prescription, rather than its original signing.</summary>
    public bool IsRepeatDispense { get; init; }
}
