using Microsoft.Extensions.Logging;
using Shared.Events;
using Shared.Infrastructure.Auth;

namespace Notification.Service.Application;

/// <summary>
/// Relays a logout recorded by ScriptFlow.API into this process's own IRevokedTokenStore, so a
/// token revoked there is also rejected here (e.g. a SignalR hub connection/reconnect using the
/// same token) - this service has no database of its own, so the shared RabbitMQ bus carries the
/// revocation instead of a shared table.
/// </summary>
public sealed class TokenRevokedEventHandler
{
    private readonly IRevokedTokenStore _revokedTokens;
    private readonly ILogger<TokenRevokedEventHandler> _logger;

    public TokenRevokedEventHandler(IRevokedTokenStore revokedTokens, ILogger<TokenRevokedEventHandler> logger)
    {
        _revokedTokens = revokedTokens;
        _logger = logger;
    }

    public async Task HandleAsync(TokenRevokedEvent revokedEvent, CancellationToken cancellationToken)
    {
        await _revokedTokens.RevokeAsync(revokedEvent.Jti, revokedEvent.ExpiresAtUtc, cancellationToken);

        _logger.LogInformation("Recorded token revocation for jti {Jti}", revokedEvent.Jti);
    }
}
