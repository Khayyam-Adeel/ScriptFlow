namespace Shared.Infrastructure.Auth;

// Tracks JWT ids (jti) revoked via logout, so a token that's still within its natural
// expiry but has been explicitly logged out of is rejected on the next request - closes
// the "a stolen token stays valid until it expires" gap. Checked by every service that
// validates these tokens (ScriptFlow.API, Notification.Service); ScriptFlow.API revokes
// locally on logout and publishes TokenRevokedEvent so the other service's store agrees.
public interface IRevokedTokenStore
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
}
