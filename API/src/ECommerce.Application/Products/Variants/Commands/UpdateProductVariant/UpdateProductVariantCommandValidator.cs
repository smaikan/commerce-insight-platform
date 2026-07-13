using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

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

        RuleFor(command => command.Color)
            .MaximumLength(80);

        RuleFor(command => command.Size)
            .MaximumLength(80);

        RuleFor(command => command.Material)
            .MaximumLength(120);
    }
}
