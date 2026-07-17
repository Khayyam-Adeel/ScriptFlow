using System.Text.Json;
using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Queries;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

public sealed class PeekDeadLetterQueueMessagesQueryHandler
    : IRequestHandler<PeekDeadLetterQueueMessagesQuery, IReadOnlyList<DeadLetterMessageDto>>
{
    private readonly IDlqRedriveService _dlqService;

    public PeekDeadLetterQueueMessagesQueryHandler(IDlqRedriveService dlqService)
    {
        _dlqService = dlqService;
    }

    public async Task<IReadOnlyList<DeadLetterMessageDto>> Handle(
        PeekDeadLetterQueueMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _dlqService.PeekAsync(request.QueueName, request.Count, cancellationToken);

        return messages.Select(m =>
        {
            var (prescriptionId, scid) = TryExtractPrescriptionFields(m.PayloadJson);
            return new DeadLetterMessageDto(m.MessageId, m.EventType, prescriptionId, scid, m.FailureReason, m.FailedAtUtc, m.PayloadJson);
        }).ToList();
    }

    // Every prescription-lifecycle event carries these two fields verbatim (see
    // Shared.contract's event DTOs), but not every dead-letterable event is prescription-shaped
    // (e.g. TokenRevokedEvent) - so a missing/unparsable payload just means "no prescription
    // context", not a failure worth surfacing to the caller.
    private static (Guid? PrescriptionId, string? Scid) TryExtractPrescriptionFields(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            Guid? prescriptionId = root.TryGetProperty("PrescriptionId", out var idProp) && idProp.TryGetGuid(out var id)
                ? id
                : null;
            string? scid = root.TryGetProperty("Scid", out var scidProp) ? scidProp.GetString() : null;

            return (prescriptionId, scid);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
