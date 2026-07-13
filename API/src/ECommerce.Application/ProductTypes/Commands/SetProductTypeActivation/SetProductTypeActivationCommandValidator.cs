using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.SetProductTypeActivation;

public sealed class SetProductTypeActivationCommandValidator : AbstractValidator<SetProductTypeActivationCommand>
{
    public SetProductTypeActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
