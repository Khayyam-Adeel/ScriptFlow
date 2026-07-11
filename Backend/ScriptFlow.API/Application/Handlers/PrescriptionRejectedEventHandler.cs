using Microsoft.Extensions.Logging;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

/// <summary>
/// Moves a prescription from Signed to Rejected once Dispatch.Worker reports the pharmacy
/// declined it, then publishes PrescriptionStatusChangedEvent so Notification.Service can
/// relay the change to the browser over SignalR.
/// </summary>
public sealed class PrescriptionRejectedEventHandler
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IProcessedMessageStore _processedMessages;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PrescriptionRejectedEventHandler> _logger;

    public PrescriptionRejectedEventHandler(
        IPrescriptionRepository prescriptions,
        IProcessedMessageStore processedMessages,
        IEventPublisher eventPublisher,
        ILogger<PrescriptionRejectedEventHandler> logger)
    {
        _prescriptions = prescriptions;
        _processedMessages = processedMessages;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionRejectedEvent rejectedEvent, CancellationToken cancellationToken)
    {
        if (await _processedMessages.IsProcessedAsync(rejectedEvent.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "PrescriptionRejectedEvent {EventId} for prescription {PrescriptionId} was already processed; skipping",
                rejectedEvent.EventId, rejectedEvent.PrescriptionId);
            return;
        }

        var prescription = await _prescriptions.GetByIdAsync(rejectedEvent.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", rejectedEvent.PrescriptionId);

        prescription.Reject();
        await _prescriptions.UpdateAsync(prescription, cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionStatusChangedEvent
        {
            PrescriptionId = prescription.Id,
            Status = PrescriptionStatus.Rejected,
            CorrelationId = rejectedEvent.CorrelationId
        }, cancellationToken);

        await _processedMessages.MarkProcessedAsync(rejectedEvent.EventId, cancellationToken);
    }
}
