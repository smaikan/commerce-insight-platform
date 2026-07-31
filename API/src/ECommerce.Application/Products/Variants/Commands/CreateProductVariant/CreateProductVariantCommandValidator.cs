using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.CreateProductVariant;

public sealed class CreateProductVariantCommandValidator : AbstractValidator<CreateProductVariantCommand>
{
    // Burada varyant ve opsiyonel açılış maliyeti alanlarının kurallarını tanımlıyorum.
    public CreateProductVariantCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Sku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Price)
            .GreaterThan(0);

        RuleFor(command => command.CompareAtPrice)
            .GreaterThanOrEqualTo(command => command.Price)
            .When(command => command.CompareAtPrice.HasValue);

        RuleFor(command => command.Stock)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.OpeningUnitCostExcludingVat)
            .GreaterThanOrEqualTo(0m)
            .PrecisionScale(18, 4, false)
            .When(command => command.OpeningUnitCostExcludingVat.HasValue);

        RuleFor(command => command.OpeningUnitCostIncludingVat)
            .GreaterThanOrEqualTo(0m)
            .PrecisionScale(18, 4, false)
            .When(command => command.OpeningUnitCostIncludingVat.HasValue);

        RuleFor(command => command.OpeningUnitCostExcludingVat)
            .Must((command, cost) =>
                command.Stock > 0 || !cost.HasValue || cost.Value == 0m)
            .WithMessage(
                "A positive opening unit cost requires positive opening stock.");

        RuleFor(command => command.OpeningUnitCostIncludingVat)
            .Must((command, cost) =>
                command.Stock > 0 || !cost.HasValue || cost.Value == 0m)
            .WithMessage(
                "A positive opening unit cost requires positive opening stock.");

        RuleFor(command => command.Barcode)
            .MaximumLength(100);

        RuleFor(command => command.Material)
            .MaximumLength(120);
    }
}
