using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Events;

namespace Shared.Infrastructure.Messaging;

public static class RabbitMqConsumerServiceCollectionExtensions
{
    // Registers IEventConsumer<TEvent> for one event type. The settings are captured in
    // the factory below (rather than registered as their own singleton) so that a service
    // consuming more than one event type - e.g. a future Notification.Service - can call
    // this once per event type without one registration's settings overwriting another's.
    public static IServiceCollection AddRabbitMqConsumer<TEvent>(this IServiceCollection services, RabbitMqConsumerSettings settings)
        where TEvent : IntegrationEvent
    {
        services.AddSingleton<IEventConsumer<TEvent>>(provider => new RabbitMqEventConsumer<TEvent>(
            provider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            settings,
            provider.GetRequiredService<ILogger<RabbitMqEventConsumer<TEvent>>>()));

        return services;
    }
}
