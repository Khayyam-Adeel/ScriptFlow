using FluentValidation;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Validators;

public sealed class MedicationLineValidator : AbstractValidator<MedicationLine>
{
    public MedicationLineValidator()
    {
        RuleFor(x => x.MedicineId).NotEmpty();
        RuleFor(x => x.TakeValue).NotEmpty();
        RuleFor(x => x.Frequency).NotEmpty();
        RuleFor(x => x.Duration).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Directions).NotEmpty();
    }
}
