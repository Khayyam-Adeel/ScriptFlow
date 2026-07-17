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

    private IConnection CreateConnection()
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

    public Task<IReadOnlyList<DeadLetterQueueSummary>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var channel = connection.CreateModel();

        var summary = KnownDeadLetterQueues.Names
            .Select(name =>
            {
                // A queue this system declares should always exist by the time this runs (every
                // consumer declares its own DLQ on startup) - but QueueDeclarePassive throws if
                // the broker somehow doesn't have it yet (e.g. that consumer has never started),
                // and one missing queue shouldn't break the whole summary view.
                try
                {
                    return new DeadLetterQueueSummary(name, (int)channel.MessageCount(name));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read message count for {QueueName}", name);
                    return new DeadLetterQueueSummary(name, 0);
                }
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterQueueSummary>>(summary);
    }

    public Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(string deadLetterQueueName, int count, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var channel = connection.CreateModel();

        var messages = new List<DeadLetterMessage>();
        var deliveryTags = new List<ulong>();

        // Collect every BasicGet first, without acking/nacking mid-loop - nacking with
        // requeue:true makes a message immediately redeliverable, so nacking inside this loop
        // could hand the same message back to a later BasicGet call and double-count it.
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = channel.BasicGet(deadLetterQueueName, autoAck: false);
            if (result is null)
            {
                break;
            }

            deliveryTags.Add(result.DeliveryTag);

            var headers = result.BasicProperties?.Headers;
            var reason = ExtractHeaderString(headers, "x-first-death-reason");
            var failedAtUtc = ExtractDeathTimestamp(headers);

            messages.Add(new DeadLetterMessage(
                result.BasicProperties?.MessageId,
                result.RoutingKey,
                reason,
                failedAtUtc,
                System.Text.Encoding.UTF8.GetString(result.Body.ToArray())));
        }

        // Requeue everything peeked, in one shot - a single multiple:true Nack up to the last
        // delivery tag covers every tag this channel has handed out below it.
        if (deliveryTags.Count > 0)
        {
            channel.BasicNack(deliveryTags[^1], multiple: true, requeue: true);
        }

        return Task.FromResult<IReadOnlyList<DeadLetterMessage>>(messages);
    }

    private static string? ExtractHeaderString(IDictionary<string, object>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            string str => str,
            _ => value.ToString()
        };
    }

    // RabbitMQ's x-death header is a list of maps, one per dead-lettering event; the most
    // recent one (x-last-death) is what we want, but only x-death is guaranteed present on
    // every broker version, so read the first entry's "time" (a Unix timestamp) from it.
    private static DateTime? ExtractDeathTimestamp(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-death", out var xDeath) || xDeath is not List<object> deaths || deaths.Count == 0)
        {
            return null;
        }

        if (deaths[0] is not IDictionary<string, object> firstDeath || !firstDeath.TryGetValue("time", out var time))
        {
            return null;
        }

        return time switch
        {
            AmqpTimestamp amqpTimestamp => DateTimeOffset.FromUnixTimeSeconds(amqpTimestamp.UnixTime).UtcDateTime,
            _ => null
        };
    }

    public Task<int> RedriveAsync(string deadLetterQueueName, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
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
