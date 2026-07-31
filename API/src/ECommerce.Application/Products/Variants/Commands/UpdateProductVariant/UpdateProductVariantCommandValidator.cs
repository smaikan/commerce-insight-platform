using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    // Burada varyant bilgi ve olası stok sayım düzeltmesi alanlarını birlikte doğruluyorum.
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Value)
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

        RuleFor(command => command.Barcode)
            .MaximumLength(100);

        RuleFor(command => command.Material)
            .MaximumLength(120);

        RuleFor(command => command.StockAdjustmentReason)
            .MaximumLength(500);
    }
}
