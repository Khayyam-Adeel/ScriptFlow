namespace Dispatch.Worker.Application.Interfaces;

// Tracks which integration event IDs have already been fully handled, so a RabbitMQ
// redelivery (e.g. after a crash between processing and acking) never dispatches the
// same prescription to the pharmacy twice.
public interface IProcessedMessageStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
