using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Nhi)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}[0-9]{4}$")
            .WithMessage("NHI must be 3 letters followed by 4 digits (e.g. ABC1234).");
    }
}
