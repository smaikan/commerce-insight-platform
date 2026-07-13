using FluentValidation;

namespace ECommerce.Application.Tags.Commands.SetTagActivation;

public sealed class SetTagActivationCommandValidator : AbstractValidator<SetTagActivationCommand>
{
    public SetTagActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
