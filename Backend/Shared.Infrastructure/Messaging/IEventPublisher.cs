using Shared.Events;

namespace Shared.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
