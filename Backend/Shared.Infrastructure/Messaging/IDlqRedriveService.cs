namespace Shared.Infrastructure.Messaging;

public interface IDlqRedriveService
{
    /// <summary>
    /// Drains every message currently sitting in the given dead-letter queue and republishes
    /// each one to the main exchange, letting whichever consumer originally owned it pick it up
    /// again as if it were a fresh delivery. Returns how many messages were redriven.
    /// </summary>
    Task<int> RedriveAsync(string deadLetterQueueName, CancellationToken cancellationToken);
}
