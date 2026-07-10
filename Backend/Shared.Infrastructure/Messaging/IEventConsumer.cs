using Shared.Events;

namespace Shared.Infrastructure.Messaging;

public interface IEventConsumer<TEvent> where TEvent : IntegrationEvent
{
    // Runs until stoppingToken is cancelled, invoking onMessage for each delivered event.
    // The caller (a BackgroundService) owns what happens when a message arrives; this
    // interface owns getting it there, acking it on success, and dead-lettering it on failure.
    Task ConsumeAsync(Func<TEvent, CancellationToken, Task> onMessage, CancellationToken stoppingToken);
}
