using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

/// <summary>
/// Operator-triggered recovery for a permanently dead-lettered message - e.g. a prescription
/// stuck at Dispatched because Dispatch.Worker's pharmacy call exhausted its Polly retries
/// (see PrescriptionSignedEventHandler) or because one of ScriptFlow.API's own lifecycle
/// consumers failed after retries. There is otherwise no automatic recovery for this short of
/// PrescriptionExpiryService's 72-hour sweep, so this exists to let an admin unstick it now.
/// </summary>
public sealed class RedriveDeadLetterQueueCommandHandler : IRequestHandler<RedriveDeadLetterQueueCommand, RedriveDeadLetterQueueResult>
{
    private readonly IDlqRedriveService _dlqRedriveService;

    public RedriveDeadLetterQueueCommandHandler(IDlqRedriveService dlqRedriveService)
    {
        _dlqRedriveService = dlqRedriveService;
    }

    public async Task<RedriveDeadLetterQueueResult> Handle(RedriveDeadLetterQueueCommand request, CancellationToken cancellationToken)
    {
        var redrivenCount = await _dlqRedriveService.RedriveAsync(request.QueueName, cancellationToken);
        return new RedriveDeadLetterQueueResult(request.QueueName, redrivenCount);
    }
}
