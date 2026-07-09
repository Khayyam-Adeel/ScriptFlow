using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class RepeatPrescriptionCommandValidator : AbstractValidator<RepeatPrescriptionCommand>
{
    public RepeatPrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
    }
}
