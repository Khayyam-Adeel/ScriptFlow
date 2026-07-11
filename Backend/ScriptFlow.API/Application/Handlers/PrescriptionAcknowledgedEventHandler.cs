using Microsoft.Extensions.Logging;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

/// <summary>
/// Moves a prescription from Signed to Acknowledged once Dispatch.Worker reports the
/// pharmacy accepted it, then publishes PrescriptionStatusChangedEvent so
/// Notification.Service can relay the change to the browser over SignalR.
/// </summary>
public sealed class PrescriptionAcknowledgedEventHandler
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IProcessedMessageStore _processedMessages;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PrescriptionAcknowledgedEventHandler> _logger;

    public PrescriptionAcknowledgedEventHandler(
        IPrescriptionRepository prescriptions,
        IProcessedMessageStore processedMessages,
        IEventPublisher eventPublisher,
        ILogger<PrescriptionAcknowledgedEventHandler> logger)
    {
        _prescriptions = prescriptions;
        _processedMessages = processedMessages;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionAcknowledgedEvent acknowledgedEvent, CancellationToken cancellationToken)
    {
        if (await _processedMessages.IsProcessedAsync(acknowledgedEvent.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "PrescriptionAcknowledgedEvent {EventId} for prescription {PrescriptionId} was already processed; skipping",
                acknowledgedEvent.EventId, acknowledgedEvent.PrescriptionId);
            return;
        }

        var prescription = await _prescriptions.GetByIdAsync(acknowledgedEvent.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", acknowledgedEvent.PrescriptionId);

        prescription.Acknowledge();
        await _prescriptions.UpdateAsync(prescription, cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionStatusChangedEvent
        {
            PrescriptionId = prescription.Id,
            Status = PrescriptionStatus.Acknowledged,
            CorrelationId = acknowledgedEvent.CorrelationId
        }, cancellationToken);

        await _processedMessages.MarkProcessedAsync(acknowledgedEvent.EventId, cancellationToken);
    }
}
