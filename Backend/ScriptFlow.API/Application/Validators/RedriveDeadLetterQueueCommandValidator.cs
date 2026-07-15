using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class RedriveDeadLetterQueueCommandValidator : AbstractValidator<RedriveDeadLetterQueueCommand>
{
    // Every DLQ RabbitMqConsumerSettings.DeadLetterQueueName currently declares across the four
    // services, kept here (rather than accepting any string) so this admin action can only ever
    // touch a queue this system actually owns, not an arbitrary broker queue.
    private static readonly HashSet<string> KnownDeadLetterQueues = new(StringComparer.Ordinal)
    {
        "dispatch.prescription-signed.dlq",
        "scriptflow-api.prescription-dispatched.dlq",
        "scriptflow-api.prescription-acknowledged.dlq",
        "scriptflow-api.prescription-rejected.dlq",
        "notification.prescription-status-changed.dlq",
        "notification.message-dead-lettered.dlq",
        "notification.token-revoked.dlq"
    };

    public RedriveDeadLetterQueueCommandValidator()
    {
        RuleFor(x => x.QueueName)
            .NotEmpty()
            .Must(name => KnownDeadLetterQueues.Contains(name))
            .WithMessage(x => $"'{x.QueueName}' is not a known dead-letter queue.");
    }
}
