using Dispatch.Worker.Application.Handlers;
using Shared.Events;
using Shared.Infrastructure.Messaging;

namespace Dispatch.Worker;

// Deliberately thin: all the RabbitMQ plumbing lives in IEventConsumer<TEvent>
// (Shared.Infrastructure) and all the dispatch decision-making lives in
// PrescriptionSignedEventHandler (Application). This class just wires the two together
// and keeps them running for the lifetime of the host.
public sealed class Worker : BackgroundService
{
    private readonly IEventConsumer<PrescriptionSignedEvent> _consumer;
    private readonly PrescriptionSignedEventHandler _handler;

    public Worker(IEventConsumer<PrescriptionSignedEvent> consumer, PrescriptionSignedEventHandler handler)
    {
        _consumer = consumer;
        _handler = handler;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _consumer.ConsumeAsync(_handler.HandleAsync, stoppingToken);
}
