namespace Shared.Events;

public sealed class TokenRevokedEvent : IntegrationEvent
{
    public required string Jti { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
