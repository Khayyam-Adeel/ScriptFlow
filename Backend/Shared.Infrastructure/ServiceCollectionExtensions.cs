using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Messaging;

namespace Shared.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.Configure<Messaging.RabbitMqOptions>(configuration.GetSection(Messaging.RabbitMqOptions.SectionName));
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddSingleton<IRevokedTokenStore, InMemoryRevokedTokenStore>();

        return services;
    }
}
