namespace Shared.Events;

// Published by RabbitMqEventConsumer<TEvent> itself, from any of the four services, the moment
// it Nacks a message to its dead-letter queue (handler failed after its own retries, or the
// message couldn't even be deserialized) - not before. Deliberately generic (no PrescriptionId):
// not every dead-lettered event is prescription-shaped (e.g. TokenRevokedEvent), so this is a
// system-wide "something failed permanently" signal, consumed by Notification.Service and
// relayed to every connected browser as a visible alert instead of only ever living in a log line.
public sealed class MessageDeadLetteredEvent : IntegrationEvent
{
    public required string EventType { get; init; }
    public required Guid FailedEventId { get; init; }
    public required string ErrorMessage { get; init; }
}
