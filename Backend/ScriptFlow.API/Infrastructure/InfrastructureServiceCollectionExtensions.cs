using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScriptFlow.API.Application.Handlers;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Infrastructure.Auth;
using ScriptFlow.API.Infrastructure.Database;
using ScriptFlow.API.Infrastructure.Persistence;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();

        // SQL Server-backed repositories, calling the stored procedures under
        // Infrastructure/Database/StoredProcedures via Dapper.
        services.AddSingleton<IPrescriptionRepository, SqlPrescriptionRepository>();
        services.AddSingleton<IPatientRepository, SqlPatientRepository>();
        services.AddSingleton<IProviderRepository, SqlProviderRepository>();
        services.AddSingleton<IMedicineRepository, SqlMedicineRepository>();
        services.AddSingleton<IPracticeRepository, SqlPracticeRepository>();
        services.AddSingleton<IPracticeLocationRepository, SqlPracticeLocationRepository>();
        services.AddSingleton<IUserRepository, SqlUserRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        AddPrescriptionOutcomeConsumers(services);

        return services;
    }

    // Consumes the two outcome events Dispatch.Worker publishes once the pharmacy gateway
    // has answered, moving Prescription.Status to Acknowledged/Rejected in SQL Server and
    // publishing PrescriptionStatusChangedEvent for Notification.Service to relay to the
    // browser. See PrescriptionAcknowledgedEventHandler/PrescriptionRejectedEventHandler.
    private static void AddPrescriptionOutcomeConsumers(IServiceCollection services)
    {
        services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();
        services.AddSingleton<PrescriptionAcknowledgedEventHandler>();
        services.AddSingleton<PrescriptionRejectedEventHandler>();

        services.AddRabbitMqConsumer<PrescriptionAcknowledgedEvent>(new RabbitMqConsumerSettings
        {
            QueueName = "scriptflow-api.prescription-acknowledged",
            RoutingKey = nameof(PrescriptionAcknowledgedEvent),
            DeadLetterQueueName = "scriptflow-api.prescription-acknowledged.dlq"
        });
        services.AddHostedService(provider => new EventConsumerBackgroundService<PrescriptionAcknowledgedEvent>(
            provider.GetRequiredService<IEventConsumer<PrescriptionAcknowledgedEvent>>(),
            provider.GetRequiredService<PrescriptionAcknowledgedEventHandler>().HandleAsync));

        services.AddRabbitMqConsumer<PrescriptionRejectedEvent>(new RabbitMqConsumerSettings
        {
            QueueName = "scriptflow-api.prescription-rejected",
            RoutingKey = nameof(PrescriptionRejectedEvent),
            DeadLetterQueueName = "scriptflow-api.prescription-rejected.dlq"
        });
        services.AddHostedService(provider => new EventConsumerBackgroundService<PrescriptionRejectedEvent>(
            provider.GetRequiredService<IEventConsumer<PrescriptionRejectedEvent>>(),
            provider.GetRequiredService<PrescriptionRejectedEventHandler>().HandleAsync));
    }
}
