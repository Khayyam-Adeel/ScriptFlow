using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Shared.Infrastructure.Messaging;

/// <summary>
/// Manual recovery path for a message that landed in a DLQ after RabbitMqEventConsumer's own
/// retries were exhausted (see that class) - there is no automatic redrive, so an operator (or
/// the admin endpoint that calls this) has to trigger one explicitly. RabbitMQ dead-letters a
/// message to its DLX under the same routing key it originally had, so re-publishing each
/// message to the main exchange under BasicGetResult.RoutingKey routes it right back to whatever
/// queue - and therefore handler - originally consumed it.
/// </summary>
public sealed class RabbitMqDlqRedriveService : IDlqRedriveService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqDlqRedriveService> _logger;

    public RabbitMqDlqRedriveService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqDlqRedriveService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<int> RedriveAsync(string deadLetterQueueName, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);

        // Snapshot the depth up front so this loop can't chase its own tail if a redriven
        // message immediately fails again and lands back on the same DLQ mid-run.
        var messageCount = channel.MessageCount(deadLetterQueueName);
        var redrivenCount = 0;

        for (var i = 0; i < messageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = channel.BasicGet(deadLetterQueueName, autoAck: false);
            if (result is null)
            {
                break;
            }

            channel.BasicPublish(_options.ExchangeName, result.RoutingKey, result.BasicProperties, result.Body);
            channel.BasicAck(result.DeliveryTag, multiple: false);
            redrivenCount++;

            _logger.LogInformation(
                "Redrove message {MessageId} ({RoutingKey}) from {DeadLetterQueue} back to {Exchange}",
                result.BasicProperties?.MessageId, result.RoutingKey, deadLetterQueueName, _options.ExchangeName);
        }

        return Task.FromResult(redrivenCount);
    }
}
