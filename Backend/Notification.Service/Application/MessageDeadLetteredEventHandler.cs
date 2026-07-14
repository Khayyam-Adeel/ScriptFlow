using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Hubs;
using Shared.Events;

namespace Notification.Service.Application;

/// <summary>
/// Relays MessageDeadLetteredEvent (published by RabbitMqEventConsumer&lt;TEvent&gt; itself,
/// from any of the four services, the moment it dead-letters a message) to every connected
/// browser as a visible system alert - previously this only ever existed as a Serilog error line
/// nobody using the app would see. No DB access, no idempotency store needed - a duplicate
/// broadcast is harmless, the client just shows the same alert twice.
/// </summary>
public sealed class MessageDeadLetteredEventHandler
{
    private readonly IHubContext<PrescriptionHub> _hubContext;
    private readonly ILogger<MessageDeadLetteredEventHandler> _logger;

    public MessageDeadLetteredEventHandler(IHubContext<PrescriptionHub> hubContext, ILogger<MessageDeadLetteredEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(MessageDeadLetteredEvent deadLetteredEvent, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.All.SendAsync(
            "messageDeadLettered",
            new
            {
                eventType = deadLetteredEvent.EventType,
                failedEventId = deadLetteredEvent.FailedEventId,
                errorMessage = deadLetteredEvent.ErrorMessage
            },
            cancellationToken);

        _logger.LogInformation(
            "Broadcast messageDeadLettered for {EventType} ({FailedEventId})",
            deadLetteredEvent.EventType, deadLetteredEvent.FailedEventId);
    }
}
