using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class SignPrescriptionCommandValidator : AbstractValidator<SignPrescriptionCommand>
{
    public SignPrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
    }
}
