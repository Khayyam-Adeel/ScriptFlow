using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;
using Shared.Events;
using Shared.Infrastructure.Correlation;

namespace Shared.Infrastructure.Messaging;

/// <summary>
/// Consumes a single event type from RabbitMQ with automatic dead-lettering: whatever the
/// caller's onMessage delegate throws after its own retries are exhausted results in a
/// Nack without requeue, which RabbitMQ routes straight to the configured dead-letter
/// queue via the main queue's "x-dead-letter-exchange" argument - no separate delayed
/// retry-queue topology is needed because retries happen inside onMessage itself.
/// </summary>
public sealed class RabbitMqEventConsumer<TEvent> : IEventConsumer<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqConsumerSettings _settings;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RabbitMqEventConsumer<TEvent>> _logger;

    public RabbitMqEventConsumer(
        IOptions<RabbitMqOptions> options,
        RabbitMqConsumerSettings settings,
        IEventPublisher eventPublisher,
        ILogger<RabbitMqEventConsumer<TEvent>> logger)
    {
        _options = options.Value;
        _settings = settings;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task ConsumeAsync(Func<TEvent, CancellationToken, Task> onMessage, CancellationToken stoppingToken)
    {
        IConnection connection;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                // Lets the Received event below be a real "async void"-free async delegate
                // instead of the plain synchronous EventingBasicConsumer callback.
                DispatchConsumersAsync = true
            };
            connection = factory.CreateConnection();
        }
        catch (Exception ex)
        {
            // Same resilience story as RabbitMqEventPublisher: a missing broker should
            // never crash the whole worker process, just mean this event type sits idle.
            _logger.LogWarning(ex,
                "Could not connect to RabbitMQ at {HostName}:{Port}; {EventType} will not be consumed",
                _options.HostName, _options.Port, typeof(TEvent).Name);
            return;
        }

        using (connection)
        using (var channel = connection.CreateModel())
        {
            DeclareTopology(channel);
            channel.BasicQos(prefetchSize: 0, prefetchCount: _settings.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += (_, delivery) => HandleDeliveryAsync(channel, delivery, onMessage, stoppingToken);

            channel.BasicConsume(_settings.QueueName, autoAck: false, consumer);
            _logger.LogInformation("Consuming {EventType} from queue {QueueName}", typeof(TEvent).Name, _settings.QueueName);

            try
            {
                // The actual work happens on RabbitMQ's own consumer thread via the
                // Received event above; this just keeps the channel/connection alive
                // for the lifetime of the BackgroundService that called us.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the host is shutting down.
            }
        }
    }

    private void DeclareTopology(IModel channel)
    {
        // Same topic exchange the publisher side declares - redeclared here defensively
        // (RabbitMQ treats a matching redeclare as a no-op) so this consumer works even
        // if it starts up before any publishing service has run.
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);

        // Dead-letter exchange + queue: where a message ends up once this consumer Nacks it.
        channel.ExchangeDeclare(_settings.DeadLetterExchangeName, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(_settings.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_settings.DeadLetterQueueName, _settings.DeadLetterExchangeName, routingKey: string.Empty);

        // The main queue: pointing it at the DLX above is what makes a Nack(requeue:false)
        // land in the dead-letter queue automatically, with no extra code on our part.
        var queueArgs = new Dictionary<string, object> { ["x-dead-letter-exchange"] = _settings.DeadLetterExchangeName };
        channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false, queueArgs);
        channel.QueueBind(_settings.QueueName, _options.ExchangeName, _settings.RoutingKey);
    }

    private async Task HandleDeliveryAsync(
        IModel channel,
        BasicDeliverEventArgs delivery,
        Func<TEvent, CancellationToken, Task> onMessage,
        CancellationToken stoppingToken)
    {
        TEvent? @event;
        try
        {
            @event = JsonSerializer.Deserialize<TEvent>(delivery.Body.Span);
            if (@event is null)
            {
                throw new JsonException($"Deserializing {typeof(TEvent).Name} produced null");
            }
        }
        catch (Exception ex)
        {
            // A message we can't even parse can never succeed on retry - straight to the DLQ.
            _logger.LogError(ex,
                "Could not deserialize {EventType} (delivery tag {DeliveryTag}); dead-lettering",
                typeof(TEvent).Name, delivery.DeliveryTag);
            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);

            var messageId = Guid.TryParse(delivery.BasicProperties?.MessageId, out var parsedId) ? parsedId : Guid.Empty;
            await PublishDeadLetterNotificationAsync(messageId, ex.Message, stoppingToken);
            return;
        }

        // From here on, every log line - including any the handler itself writes - carries
        // the correlation ID that started with the original HTTP request, the same way
        // CorrelationIdMiddleware threads it through ScriptFlow.API's HTTP pipeline.
        CorrelationIdAccessor.Set(@event.CorrelationId);
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        {
            try
            {
                await onMessage(@event, stoppingToken);
                channel.BasicAck(delivery.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler for {EventType} ({EventId}) failed after its own retries; dead-lettering",
                    typeof(TEvent).Name, @event.EventId);
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);

                await PublishDeadLetterNotificationAsync(@event.EventId, ex.Message, stoppingToken);
            }
        }
    }

    // Best-effort, same as every other publish in this codebase: RabbitMqEventPublisher already
    // swallows its own failures (a broker outage here must not mask the *original* dead-lettering
    // that already happened above), so this can never throw and never turns a successful
    // dead-letter into an unhandled exception.
    private Task PublishDeadLetterNotificationAsync(Guid failedEventId, string errorMessage, CancellationToken cancellationToken)
        => _eventPublisher.PublishAsync(new MessageDeadLetteredEvent
        {
            EventType = typeof(TEvent).Name,
            FailedEventId = failedEventId,
            ErrorMessage = errorMessage
        }, cancellationToken);
}
