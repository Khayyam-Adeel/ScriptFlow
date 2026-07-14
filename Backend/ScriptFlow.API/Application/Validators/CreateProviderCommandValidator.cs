using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.NzmcNo).NotEmpty();
        RuleFor(x => x.PracticeLocationId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^[0-9+\-\s()]{7,20}$")
            .WithMessage("Phone number must be 7-20 characters and may only contain digits, spaces, and + - ( ).");
        RuleFor(x => x.Qualification).NotEmpty();
    }
}
