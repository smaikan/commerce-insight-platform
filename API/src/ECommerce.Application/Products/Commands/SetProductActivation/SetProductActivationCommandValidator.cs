using FluentValidation;

namespace ECommerce.Application.Products.Commands.SetProductActivation;

public sealed class SetProductActivationCommandValidator : AbstractValidator<SetProductActivationCommand>
{
    public SetProductActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
