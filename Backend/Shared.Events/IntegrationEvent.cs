namespace Shared.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
}
