using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;

public sealed class UpdateProductVariantPriceCommandValidator : AbstractValidator<UpdateProductVariantPriceCommand>
{
    public UpdateProductVariantPriceCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Price)
            .GreaterThan(0);

        RuleFor(command => command.CompareAtPrice)
            .GreaterThanOrEqualTo(command => command.Price)
            .When(command => command.CompareAtPrice.HasValue);
    }
}
