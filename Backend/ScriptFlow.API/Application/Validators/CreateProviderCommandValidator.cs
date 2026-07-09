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
    }
}
