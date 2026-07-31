using ECommerce.Application.Common.Services;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    // Burada ürün oluşturma isteğinin alan ve ilişki kurallarını tanımlıyorum.
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(command => command.MainSku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.TypeId)
            .Must(typeId => !typeId.HasValue || typeId.Value != Guid.Empty)
            .WithMessage("Product type id cannot be empty.");

        RuleFor(command => command.BrandId)
            .Must(brandId => !brandId.HasValue || brandId.Value != Guid.Empty)
            .WithMessage("Brand id cannot be empty.");

        RuleFor(command => command.TaxRateId)
            .Must(taxRateId => !taxRateId.HasValue || taxRateId.Value != Guid.Empty)
            .WithMessage("Tax rate id cannot be empty.");

        RuleFor(command => command.Url)
            .MaximumLength(250);

        RuleFor(command => command.Description)
            .MaximumLength(4000);

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.SeoTitle)
            .MaximumLength(250);

        RuleFor(command => command.SeoDescription)
            .MaximumLength(500);

        RuleForEach(command => command.CollectionIds)
            .NotEmpty();

        RuleFor(command => command.CollectionIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Collection ids cannot contain duplicates.");

        RuleForEach(command => command.Tags)
            .NotEmpty()
            .MaximumLength(ProductTagRules.MaximumTagNameLength);

        RuleFor(command => command.Tags)
            .Must(tags => tags is null || tags.Count <= ProductTagRules.MaximumTagsPerProduct)
            .WithMessage($"A product can contain at most {ProductTagRules.MaximumTagsPerProduct} tags.");

        RuleFor(command => command.Variants)
            .NotEmpty()
            .WithMessage("A product must have at least one variant.");

        RuleForEach(command => command.Variants)
            .ChildRules(variant =>
            {
                variant.RuleFor(item => item.Name)
                    .NotEmpty()
                    .MaximumLength(150);

                variant.RuleFor(item => item.Sku)
                    .NotEmpty()
                    .MaximumLength(100);

                variant.RuleFor(item => item.Price)
                    .GreaterThan(0);

                variant.RuleFor(item => item.Stock)
                    .GreaterThanOrEqualTo(0);

                variant.RuleFor(item => item.OpeningUnitCostExcludingVat)
                    .GreaterThanOrEqualTo(0m)
                    .PrecisionScale(18, 4, false)
                    .When(item => item.OpeningUnitCostExcludingVat.HasValue);

                variant.RuleFor(item => item.OpeningUnitCostIncludingVat)
                    .GreaterThanOrEqualTo(0m)
                    .PrecisionScale(18, 4, false)
                    .When(item => item.OpeningUnitCostIncludingVat.HasValue);

                variant.RuleFor(item => item.OpeningUnitCostExcludingVat)
                    .Must((item, cost) =>
                        item.Stock > 0 || !cost.HasValue || cost.Value == 0m)
                    .WithMessage(
                        "A positive opening unit cost requires positive opening stock.");

                variant.RuleFor(item => item.OpeningUnitCostIncludingVat)
                    .Must((item, cost) =>
                        item.Stock > 0 || !cost.HasValue || cost.Value == 0m)
                    .WithMessage(
                        "A positive opening unit cost requires positive opening stock.");

                variant.RuleFor(item => item.CompareAtPrice)
                    .GreaterThanOrEqualTo(item => item.Price)
                    .When(item => item.CompareAtPrice.HasValue);

                variant.RuleFor(item => item.Barcode)
                    .MaximumLength(100);

                variant.RuleFor(item => item.Material)
                    .MaximumLength(120);
            });
    }
}
