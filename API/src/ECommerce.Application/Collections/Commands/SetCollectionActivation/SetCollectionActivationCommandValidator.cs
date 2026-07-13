using FluentValidation;

namespace ECommerce.Application.Collections.Commands.SetCollectionActivation;

public sealed class SetCollectionActivationCommandValidator : AbstractValidator<SetCollectionActivationCommand>
{
    public SetCollectionActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
