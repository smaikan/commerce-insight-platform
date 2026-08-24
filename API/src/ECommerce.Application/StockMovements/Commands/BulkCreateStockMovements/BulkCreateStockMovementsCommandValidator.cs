using ECommerce.Application.StockMovements.Common;
using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;

public sealed class BulkCreateStockMovementsCommandValidator
    : AbstractValidator<BulkCreateStockMovementsCommand>
{
    // Burada toplu isteğin boyutunu ve içindeki her stok hareketini doğruluyorum.
    public BulkCreateStockMovementsCommandValidator()
    {
        RuleFor(command => command.Movements)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(movements => movements.Count <= BulkCreateStockMovementsCommand.MaximumBatchSize)
            .WithMessage(
                $"A stock movement batch cannot contain more than " +
                $"{BulkCreateStockMovementsCommand.MaximumBatchSize} items.");

        RuleForEach(command => command.Movements)
            .NotNull()
            .SetValidator(new BulkStockMovementItemValidator());
    }
}

internal sealed class BulkStockMovementItemValidator : AbstractValidator<BulkStockMovementItem>
{
    // Burada toplu listedeki hareketin varyantını, türünü, yönünü ve gerekçesini doğruluyorum.
    public BulkStockMovementItemValidator()
    {
        RuleFor(item => item.ProductVariantSku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(item => item.QuantityDelta)
            .NotEqual(0)
            .NotEqual(int.MinValue)
            .WithMessage("Quantity is outside the supported range.");

        RuleFor(item => item.Type)
            .IsInEnum()
            .Must(AdministrativeStockMovementRules.IsAllowedType)
            .WithMessage("This stock movement type cannot be created manually.");

        RuleFor(item => item)
            .Must(item => AdministrativeStockMovementRules.HasCompatibleDirection(
                item.Type,
                item.QuantityDelta))
            .WithMessage("Stock movement type and quantity direction are not compatible.");

        RuleFor(item => item.Reason)
            .MaximumLength(StockMovement.MaximumReasonLength);
    }
}
