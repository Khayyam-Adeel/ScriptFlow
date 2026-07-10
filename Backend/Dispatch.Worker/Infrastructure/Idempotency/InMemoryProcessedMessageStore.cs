using System.Collections.Concurrent;
using Dispatch.Worker.Application.Interfaces;

namespace Dispatch.Worker.Infrastructure.Idempotency;

// In-memory idempotency ledger, matching ScriptFlow.API's current in-memory-only
// persistence. Limitation: this resets on restart, so a message processed just before a
// crash could be reprocessed once after restart. The durable replacement is the
// ProcessedMessages table documented in SPEC/DatabaseSpec.md, to be wired up once SQL
// Server persistence lands across the solution.
public sealed class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<Guid, byte> _processedEventIds = new();

    public Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
        => Task.FromResult(_processedEventIds.ContainsKey(eventId));

    public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        _processedEventIds[eventId] = 0;
        return Task.CompletedTask;
    }
}
