using FluentValidation;
using ScriptFlow.API.Application.Queries;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Validators;

public sealed class PeekDeadLetterQueueMessagesQueryValidator : AbstractValidator<PeekDeadLetterQueueMessagesQuery>
{
    public PeekDeadLetterQueueMessagesQueryValidator()
    {
        RuleFor(x => x.QueueName)
            .NotEmpty()
            .Must(name => KnownDeadLetterQueues.Names.Contains(name))
            .WithMessage(x => $"'{x.QueueName}' is not a known dead-letter queue.");

        RuleFor(x => x.Count).InclusiveBetween(1, 200);
    }
}
