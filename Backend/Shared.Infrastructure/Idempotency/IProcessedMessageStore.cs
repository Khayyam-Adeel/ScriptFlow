namespace Shared.Infrastructure.Idempotency;

// Tracks which integration event IDs have already been fully handled, so a RabbitMQ
// redelivery (e.g. after a crash between processing and acking) never repeats the
// side effect (a pharmacy call, a DB status write, ...) for the same event twice.
public interface IProcessedMessageStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, string eventType, Guid prescriptionId, CancellationToken cancellationToken);
}
