using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PracticeLocationId).NotEmpty();
        RuleFor(x => x.Medications).NotEmpty().WithMessage("A prescription must have at least one medication.");
        RuleForEach(x => x.Medications).SetValidator(new MedicationLineValidator());
    }
}
