using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScriptFlow.API.Application.Interfaces;
using Shared.contract.Enums;
using Shared.Events;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Infrastructure.Expiry;

/// <summary>
/// One sweep of the Expired transition: every prescription still Created, Signed, or Dispatched
/// past PrescriptionExpiryOptions.StaleAfterHours gets Prescription.Expire() applied and a
/// PrescriptionStatusChangedEvent published, the same way the Dispatched/Acknowledged/Rejected
/// transitions do for Notification.Service's live status board. Driven on a timer by
/// PrescriptionExpiryBackgroundService, not MediatR - same "plain class reacting to a
/// non-HTTP trigger" shape as PrescriptionAcknowledgedEventHandler/PrescriptionRejectedEventHandler.
/// </summary>
public sealed class PrescriptionExpiryService
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOptions<PrescriptionExpiryOptions> _options;
    private readonly ILogger<PrescriptionExpiryService> _logger;

    public PrescriptionExpiryService(
        IPrescriptionRepository prescriptions,
        IEventPublisher eventPublisher,
        IOptions<PrescriptionExpiryOptions> options,
        ILogger<PrescriptionExpiryService> logger)
    {
        _prescriptions = prescriptions;
        _eventPublisher = eventPublisher;
        _options = options;
        _logger = logger;
    }

    public async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(-_options.Value.StaleAfterHours);
        var stale = await _prescriptions.GetStaleForExpiryAsync(cutoff, cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Expiring {Count} stale prescription(s) created before {Cutoff}", stale.Count, cutoff);

        foreach (var prescription in stale)
        {
            prescription.Expire();
            await _prescriptions.UpdateAsync(prescription, cancellationToken);

            await _eventPublisher.PublishAsync(new PrescriptionStatusChangedEvent
            {
                PrescriptionId = prescription.Id,
                Status = PrescriptionStatus.Expired,
                CorrelationId = Guid.NewGuid().ToString()
            }, cancellationToken);
        }
    }
}
