using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandValidator : AbstractValidator<UpdateProductVariantStockCommand>
{
    // Burada stok düzeltme isteğinin güvenli sayı aralığında olduğunu doğruluyorum.
    public UpdateProductVariantStockCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Quantity)
            .NotEqual(0)
            .NotEqual(int.MinValue)
            .WithMessage("Quantity is outside the supported range.");

        RuleFor(command => command.Reason)
            .MaximumLength(500);
    }
}
