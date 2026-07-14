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
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^[0-9+\-\s()]{7,20}$")
            .WithMessage("Phone number must be 7-20 characters and may only contain digits, spaces, and + - ( ).");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
