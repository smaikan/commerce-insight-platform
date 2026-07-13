using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;

public sealed class SetProductVariantActivationCommandValidator : AbstractValidator<SetProductVariantActivationCommand>
{
    public SetProductVariantActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
