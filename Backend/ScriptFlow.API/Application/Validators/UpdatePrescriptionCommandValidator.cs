using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class UpdatePrescriptionCommandValidator : AbstractValidator<UpdatePrescriptionCommand>
{
    public UpdatePrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
        RuleFor(x => x.Medications).NotEmpty().WithMessage("A prescription must have at least one medication.");
        RuleForEach(x => x.Medications).SetValidator(new MedicationLineValidator());
    }
}
