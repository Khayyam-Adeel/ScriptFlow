namespace Shared.Infrastructure.Messaging;

public sealed record DeadLetterQueueSummary(string QueueName, int MessageCount);

public sealed record DeadLetterMessage(
    string? MessageId,
    string EventType,
    string? FailureReason,
    DateTime? FailedAtUtc,
    string PayloadJson);

public interface IDlqRedriveService
{
    /// <summary>
    /// Message count for every known dead-letter queue - the "how much is stuck" view an admin
    /// page lists before drilling into any one queue.
    /// </summary>
    Task<IReadOnlyList<DeadLetterQueueSummary>> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Non-destructively inspects up to <paramref name="count"/> messages currently sitting in
    /// the given dead-letter queue - each is nacked with requeue:true after reading, so peeking
    /// never consumes or reorders what redrive would later process.
    /// </summary>
    Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(string deadLetterQueueName, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Drains every message currently sitting in the given dead-letter queue and republishes
    /// each one to the main exchange, letting whichever consumer originally owned it pick it up
    /// again as if it were a fresh delivery. Returns how many messages were redriven.
    /// </summary>
    Task<int> RedriveAsync(string deadLetterQueueName, CancellationToken cancellationToken);
}
