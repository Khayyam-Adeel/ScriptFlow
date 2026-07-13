using Shared.contract.Enums;

namespace Shared.Events;

// Published by ScriptFlow.API once a prescription's status has actually been written to
// SQL Server (Dispatched/Acknowledged/Rejected/Expired). Notification.Service consumes this to
// relay the change to connected browsers over SignalR - it is the one event type external to
// the Dispatch.Worker -> pharmacy pipeline, decoupling "who mutated the data" from "who tells
// the UI about it".
public sealed class PrescriptionStatusChangedEvent : IntegrationEvent
{
    public required Guid PrescriptionId { get; init; }
    public required PrescriptionStatus Status { get; init; }
}
