namespace Shared.Infrastructure.Messaging;

/// <summary>
/// Every DLQ RabbitMqConsumerSettings.DeadLetterQueueName currently declares across the four
/// services - single source of truth shared by the redrive allow-list validator and the
/// DLQ summary/peek admin endpoints, so a queue can only ever be added here once.
/// </summary>
public static class KnownDeadLetterQueues
{
    public static readonly IReadOnlyCollection<string> Names = new[]
    {
        "dispatch.prescription-signed.dlq",
        "scriptflow-api.prescription-dispatched.dlq",
        "scriptflow-api.prescription-acknowledged.dlq",
        "scriptflow-api.prescription-rejected.dlq",
        "notification.prescription-status-changed.dlq",
        "notification.message-dead-lettered.dlq",
        "notification.token-revoked.dlq"
    };
}
