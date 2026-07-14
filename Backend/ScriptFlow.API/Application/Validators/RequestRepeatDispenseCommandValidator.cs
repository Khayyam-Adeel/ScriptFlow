using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class RequestRepeatDispenseCommandValidator : AbstractValidator<RequestRepeatDispenseCommand>
{
    public RequestRepeatDispenseCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
    }
}
