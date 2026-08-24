using FluentValidation;
using ECommerce.Application.StockMovements.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandValidator : AbstractValidator<UpdateProductVariantStockCommand>
{
    // Burada stok hareketinin izinli yönetim türü, yönü ve açıklamasıyla tutarlı olduğunu doğruluyorum.
    public UpdateProductVariantStockCommandValidator()
    {
        RuleFor(command => command.ProductVariantSku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.QuantityDelta)
            .NotEqual(0)
            .NotEqual(int.MinValue)
            .WithMessage("Quantity is outside the supported range.");

        RuleFor(command => command.Type)
            .IsInEnum()
            .Must(AdministrativeStockMovementRules.IsAllowedType)
            .WithMessage("This stock movement type cannot be created manually.");

        RuleFor(command => command)
            .Must(command => AdministrativeStockMovementRules.HasCompatibleDirection(
                command.Type,
                command.QuantityDelta))
            .WithMessage("Stock movement type and quantity direction are not compatible.");

        RuleFor(command => command.Reason)
            .MaximumLength(StockMovement.MaximumReasonLength);
    }
}
