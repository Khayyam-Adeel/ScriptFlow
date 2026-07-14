using FluentValidation;
using ScriptFlow.API.Application.Commands;

namespace ScriptFlow.API.Application.Validators;

public sealed class CreatePracticeCommandValidator : AbstractValidator<CreatePracticeCommand>
{
    public CreatePracticeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
