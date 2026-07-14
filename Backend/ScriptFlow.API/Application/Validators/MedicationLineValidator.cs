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

        // Optional clinical detail - only length-bounded (matching the tvpMedicationLine / table
        // column widths), never required.
        RuleFor(x => x.Route).MaximumLength(100);
        RuleFor(x => x.Strength).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Repeats).GreaterThanOrEqualTo(0);
    }
}
