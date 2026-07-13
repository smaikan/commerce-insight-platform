using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandValidator : AbstractValidator<UpdateProductVariantStockCommand>
{
    public UpdateProductVariantStockCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Stock)
            .GreaterThanOrEqualTo(0);
    }
}
