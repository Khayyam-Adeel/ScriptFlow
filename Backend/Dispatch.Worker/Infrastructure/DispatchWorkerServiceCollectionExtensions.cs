using Dispatch.Worker.Application.Handlers;
using Dispatch.Worker.Application.Interfaces;
using Dispatch.Worker.Infrastructure.Pharmacy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace Dispatch.Worker.Infrastructure;

public static class DispatchWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddDispatchWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PharmacyGatewayOptions>(configuration.GetSection(PharmacyGatewayOptions.SectionName));

        services.AddSingleton<IProcessedMessageStore, SqlProcessedMessageStore>();
        services.AddSingleton<PrescriptionSignedEventHandler>();

        services.AddHttpClient<IPharmacyGatewayClient, PharmacyGatewayHttpClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PharmacyGatewayOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler(BuildRetryPolicy());

        // The worker only consumes one event type today, but each event type gets its own
        // queue/DLQ pair via AddRabbitMqConsumer, so a second one (e.g. for repeat-prescription
        // dispatch) can be registered later without touching this one.
        services.AddRabbitMqConsumer<PrescriptionSignedEvent>(new RabbitMqConsumerSettings
        {
            QueueName = "dispatch.prescription-signed",
            RoutingKey = nameof(PrescriptionSignedEvent),
            DeadLetterQueueName = "dispatch.prescription-signed.dlq"
        });

        return services;
    }

    // Retries a dropped/failed pharmacy call 3 times with exponential backoff (2s, 4s, 8s)
    // before giving up - this is what DeliveryServiceSpec.md's "Retry, Backoff" non-functional
    // requirement maps to. HandleTransientHttpError covers 5xx/408 responses and
    // HttpRequestException; the mock gateway's simulated "drop" (HttpContext.Abort()) surfaces
    // to this client as an HttpRequestException, which is what triggers a retry here.
    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
