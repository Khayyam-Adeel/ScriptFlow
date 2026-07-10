namespace Shared.Infrastructure.Messaging;

// Per-consumer RabbitMQ topology, supplied by the consuming service at startup - unlike
// RabbitMqOptions (broker connection details shared by every service), each event
// consumer picks its own queue name, routing key, and dead-letter queue.
public sealed class RabbitMqConsumerSettings
{
    public required string QueueName { get; init; }
    public required string RoutingKey { get; init; }
    public required string DeadLetterQueueName { get; init; }

    // One shared dead-letter exchange is enough: RabbitMQ routes a dead-lettered message
    // to whichever queue is bound under its own original routing key, so every service's
    // DLQ can hang off the same exchange without messages crossing into each other's DLQ.
    public string DeadLetterExchangeName { get; init; } = "scriptflow.events.dlx";

    // How many unacknowledged messages this consumer will hold at once. Kept low (not 0 =
    // unlimited) so one slow pharmacy call doesn't let RabbitMQ hand out a pile of other
    // messages that then all sit waiting behind it.
    public ushort PrefetchCount { get; init; } = 10;
}
