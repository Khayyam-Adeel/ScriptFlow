using Dispatch.Worker.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.contract.Contracts;
using Shared.contract.Enums;
using Shared.Events;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Messaging;

namespace Dispatch.Worker.Application.Handlers;

/// <summary>
/// Orchestrates one PrescriptionSignedEvent: skip it if already processed, call the
/// pharmacy gateway, and publish whichever outcome event matches its answer. Retry with
/// backoff for transient pharmacy failures lives in the HTTP client (Polly), not here -
/// if DispatchAsync throws, its retries are already exhausted, and this method lets the
/// exception propagate so the RabbitMQ consumer dead-letters the message.
/// </summary>
public sealed class PrescriptionSignedEventHandler
{
    private readonly IPharmacyGatewayClient _pharmacyGatewayClient;
    private readonly IProcessedMessageStore _processedMessages;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PrescriptionSignedEventHandler> _logger;

    public PrescriptionSignedEventHandler(
        IPharmacyGatewayClient pharmacyGatewayClient,
        IProcessedMessageStore processedMessages,
        IEventPublisher eventPublisher,
        ILogger<PrescriptionSignedEventHandler> logger)
    {
        _pharmacyGatewayClient = pharmacyGatewayClient;
        _processedMessages = processedMessages;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionSignedEvent signedEvent, CancellationToken cancellationToken)
    {
        // Idempotency: RabbitMQ can redeliver a message that was never acked (e.g. the
        // worker crashed mid-processing), and we must not call the pharmacy twice for it.
        if (await _processedMessages.IsProcessedAsync(signedEvent.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "PrescriptionSignedEvent {EventId} for prescription {PrescriptionId} was already processed; skipping",
                signedEvent.EventId, signedEvent.PrescriptionId);
            return;
        }

        // Signed -> Dispatched happens the moment delivery is attempted, before the pharmacy
        // has answered - ScriptFlow.API's PrescriptionDispatchedEventHandler applies this to
        // the aggregate so the live status board shows "Dispatched" while the (possibly slow,
        // possibly retried) call to the pharmacy gateway below is still in flight.
        await _eventPublisher.PublishAsync(new PrescriptionDispatchedEvent
        {
            PrescriptionId = signedEvent.PrescriptionId,
            Scid = signedEvent.Scid,
            DispatchedAtUtc = DateTime.UtcNow,
            Status = PrescriptionStatus.Dispatched,
            CorrelationId = signedEvent.CorrelationId
        }, cancellationToken);

        var request = new PharmacyDispatchRequest
        {
            PrescriptionId = signedEvent.PrescriptionId,
            Scid = signedEvent.Scid,
            CorrelationId = signedEvent.CorrelationId
        };

        // Any exception here (including the HTTP client's own Polly retries finally
        // giving up) is intentionally left to propagate - see the class comment above.
        var response = await _pharmacyGatewayClient.DispatchAsync(request, cancellationToken);

        if (response.Outcome == PharmacyDispatchOutcome.Acknowledged)
        {
            await _eventPublisher.PublishAsync(new PrescriptionAcknowledgedEvent
            {
                PrescriptionId = signedEvent.PrescriptionId,
                Scid = signedEvent.Scid,
                PharmacyReference = response.PharmacyReference!.Value,
                AcknowledgedAtUtc = DateTime.UtcNow,
                Status = PrescriptionStatus.Acknowledged,
                CorrelationId = signedEvent.CorrelationId
            }, cancellationToken);
        }
        else
        {
            await _eventPublisher.PublishAsync(new PrescriptionRejectedEvent
            {
                PrescriptionId = signedEvent.PrescriptionId,
                Scid = signedEvent.Scid,
                RejectionReason = response.RejectionReason!,
                RejectedAtUtc = DateTime.UtcNow,
                Status = PrescriptionStatus.Rejected,
                CorrelationId = signedEvent.CorrelationId
            }, cancellationToken);
        }

        // Only mark processed once the outcome has actually been published - if publishing
        // itself throws, redelivery should retry the whole thing, not skip it as done.
        await _processedMessages.MarkProcessedAsync(signedEvent.EventId, cancellationToken);
    }
}
