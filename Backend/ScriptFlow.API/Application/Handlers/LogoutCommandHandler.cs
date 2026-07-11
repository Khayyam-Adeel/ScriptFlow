using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.Interfaces;
using Shared.Events;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IRevokedTokenStore _revokedTokens;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public LogoutCommandHandler(
        ICurrentUserAccessor currentUser,
        IRevokedTokenStore revokedTokens,
        IEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _currentUser = currentUser;
        _revokedTokens = revokedTokens;
        _eventPublisher = eventPublisher;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var jti = _currentUser.CurrentJti;
        var expiresAtUtc = _currentUser.CurrentTokenExpiresAtUtc;

        await _revokedTokens.RevokeAsync(jti, expiresAtUtc, cancellationToken);

        await _eventPublisher.PublishAsync(new TokenRevokedEvent
        {
            Jti = jti,
            ExpiresAtUtc = expiresAtUtc,
            CorrelationId = _correlationIdAccessor.CorrelationId
        }, cancellationToken);
    }
}
