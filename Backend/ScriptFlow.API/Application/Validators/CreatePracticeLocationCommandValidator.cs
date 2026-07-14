using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class CreatePracticeLocationCommandValidator : AbstractValidator<CreatePracticeLocationCommand>
{
    public CreatePracticeLocationCommandValidator()
    {
        RuleFor(x => x.PracticeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.HpiNo)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}[0-9]{2}$")
            .WithMessage("HPI No must be 3 letters followed by 2 digits (e.g. FZZ99).");
        RuleFor(x => x.HpiExtension)
            .NotEmpty()
            .Matches("^[A-Za-z]$")
            .WithMessage("HPI Extension must be a single letter (e.g. B).");
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^[0-9+\-\s()]{7,20}$")
            .WithMessage("Phone number must be 7-20 characters and may only contain digits, spaces, and + - ( ).");
    }
}
