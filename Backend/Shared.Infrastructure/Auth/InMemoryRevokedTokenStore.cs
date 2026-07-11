using System.Collections.Concurrent;

namespace Shared.Infrastructure.Auth;

// In-memory revocation ledger, one instance per process (ScriptFlow.API, Notification.Service
// each register their own). Same limitation as InMemoryProcessedMessageStore: it resets on
// restart, so a token revoked just before a crash/redeploy would be accepted again until it
// naturally expires. Acceptable given the existing precedent for this store shape and the
// short (60-minute default) token lifetime; a durable replacement would move this into SQL
// Server alongside the other persisted state.
public sealed class InMemoryRevokedTokenStore : IRevokedTokenStore
{
    private readonly ConcurrentDictionary<string, DateTime> _revokedJtiExpiries = new();

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (!_revokedJtiExpiries.TryGetValue(jti, out var expiresAtUtc))
        {
            return Task.FromResult(false);
        }

        // Self-cleaning: once the token would have expired naturally anyway, there's no
        // more reason to remember the revocation, so drop it instead of growing forever.
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            _revokedJtiExpiries.TryRemove(jti, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        _revokedJtiExpiries[jti] = expiresAtUtc;
        return Task.CompletedTask;
    }
}
