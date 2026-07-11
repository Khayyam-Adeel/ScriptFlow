using Microsoft.Extensions.Hosting;
using Shared.Events;

namespace Shared.Infrastructure.Messaging;

// Generic version of the thin "BackgroundService that just awaits IEventConsumer.ConsumeAsync"
// wrapper Dispatch.Worker's own Worker.cs hand-writes for its one event type - useful once a
// service consumes more than one event type (ScriptFlow.API's Acknowledged/Rejected consumers,
// Notification.Service's status-changed consumer) so each doesn't need its own near-identical class.
public sealed class EventConsumerBackgroundService<TEvent> : BackgroundService
    where TEvent : IntegrationEvent
{
    private readonly IEventConsumer<TEvent> _consumer;
    private readonly Func<TEvent, CancellationToken, Task> _onMessage;

    public EventConsumerBackgroundService(IEventConsumer<TEvent> consumer, Func<TEvent, CancellationToken, Task> onMessage)
    {
        _consumer = consumer;
        _onMessage = onMessage;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _consumer.ConsumeAsync(_onMessage, stoppingToken);
}
