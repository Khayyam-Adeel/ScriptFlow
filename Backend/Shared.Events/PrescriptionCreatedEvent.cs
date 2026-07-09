using Shared.contract.Enums;

namespace Shared.Events;

public sealed class PrescriptionCreatedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }
    public required Guid PatientId { get; init; }
    public required Guid ProviderId { get; init; }
    public required PrescriptionStatus Status { get; init; }
}
