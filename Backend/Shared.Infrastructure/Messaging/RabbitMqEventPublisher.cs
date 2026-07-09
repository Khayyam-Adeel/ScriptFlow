using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Events;

namespace Shared.Infrastructure.Messaging;

/// <summary>
/// Publishes to RabbitMQ on a best-effort basis: a broker outage is logged, never thrown,
/// so a failed publish never fails the HTTP request that triggered it (no outbox pattern yet).
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly Lazy<IConnection?> _connection;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connection = new Lazy<IConnection?>(CreateConnection);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent).Name;

        try
        {
            var connection = _connection.Value;
            if (connection is null)
            {
                _logger.LogWarning(
                    "Skipped publishing {EventType} ({EventId}): no RabbitMQ connection available",
                    eventType, @event.EventId);
                return Task.CompletedTask;
            }

            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);

            var body = JsonSerializer.SerializeToUtf8Bytes((object)@event);
            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.MessageId = @event.EventId.ToString();
            properties.CorrelationId = @event.CorrelationId;
            properties.Persistent = true;

            channel.BasicPublish(_options.ExchangeName, routingKey: eventType, properties, body);

            _logger.LogInformation(
                "Published {EventType} ({EventId}) to exchange {Exchange}",
                eventType, @event.EventId, _options.ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish {EventType} ({EventId}); continuing without failing the request",
                eventType, @event.EventId);
        }

        return Task.CompletedTask;
    }

    private IConnection? CreateConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };
            return factory.CreateConnection();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not connect to RabbitMQ at {HostName}:{Port}; events will be logged but not published",
                _options.HostName, _options.Port);
            return null;
        }
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value?.Dispose();
        }
    }
}
