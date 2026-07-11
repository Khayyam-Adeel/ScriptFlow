using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Hubs;
using Shared.Events;

namespace Notification.Service.Application;

/// <summary>
/// Relays PrescriptionStatusChangedEvent (published by ScriptFlow.API once a status write
/// has actually landed in SQL Server) to every connected browser over SignalR. No DB access,
/// no idempotency store needed here - a duplicate broadcast is harmless, the client just
/// re-applies the same status to the same prescription row.
/// </summary>
public sealed class PrescriptionStatusChangedEventHandler
{
    private readonly IHubContext<PrescriptionHub> _hubContext;
    private readonly ILogger<PrescriptionStatusChangedEventHandler> _logger;

    public PrescriptionStatusChangedEventHandler(IHubContext<PrescriptionHub> hubContext, ILogger<PrescriptionStatusChangedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionStatusChangedEvent statusChangedEvent, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.All.SendAsync(
            "prescriptionStatusChanged",
            new { prescriptionId = statusChangedEvent.PrescriptionId, status = statusChangedEvent.Status },
            cancellationToken);

        _logger.LogInformation(
            "Broadcast prescriptionStatusChanged for {PrescriptionId} -> {Status}",
            statusChangedEvent.PrescriptionId, statusChangedEvent.Status);
    }
}
