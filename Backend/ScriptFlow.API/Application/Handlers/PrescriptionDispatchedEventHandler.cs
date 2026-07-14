using Microsoft.Extensions.Logging;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

/// <summary>
/// Moves a prescription from Signed to Dispatched once Dispatch.Worker starts attempting
/// delivery to the pharmacy, then publishes PrescriptionStatusChangedEvent so
/// Notification.Service can relay the change to the browser over SignalR.
/// </summary>
public sealed class PrescriptionDispatchedEventHandler
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IProcessedMessageStore _processedMessages;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PrescriptionDispatchedEventHandler> _logger;

    public PrescriptionDispatchedEventHandler(
        IPrescriptionRepository prescriptions,
        IProcessedMessageStore processedMessages,
        IEventPublisher eventPublisher,
        ILogger<PrescriptionDispatchedEventHandler> logger)
    {
        _prescriptions = prescriptions;
        _processedMessages = processedMessages;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionDispatchedEvent dispatchedEvent, CancellationToken cancellationToken)
    {
        if (await _processedMessages.IsProcessedAsync(dispatchedEvent.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "PrescriptionDispatchedEvent {EventId} for prescription {PrescriptionId} was already processed; skipping",
                dispatchedEvent.EventId, dispatchedEvent.PrescriptionId);
            return;
        }

        var prescription = await _prescriptions.GetByIdAsync(dispatchedEvent.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", dispatchedEvent.PrescriptionId);

        prescription.Dispatch();
        await _prescriptions.UpdateAsync(prescription, cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionStatusChangedEvent
        {
            PrescriptionId = prescription.Id,
            Status = PrescriptionStatus.Dispatched,
            CorrelationId = dispatchedEvent.CorrelationId
        }, cancellationToken);

        await _processedMessages.MarkProcessedAsync(
            dispatchedEvent.EventId, nameof(PrescriptionDispatchedEvent), dispatchedEvent.PrescriptionId, cancellationToken);
    }
}
