using ECommerce.Application.Common.Services;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsCommandValidator : AbstractValidator<BulkCreateProductsCommand>
{
    // Burada toplu ürün isteğinin ürün, varyant ve görsel kurallarını tanımlıyorum.
    public BulkCreateProductsCommandValidator()
    {
        RuleFor(command => command.Products)
            .NotEmpty()
            .Must(products => products is not null && products.Count <= 500)
            .WithMessage("A bulk product request can contain at most 500 products.");

        RuleFor(command => command.Products)
            .Must(products => products is null ||
                products
                    .SelectMany(product => product.Tags ?? [])
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() <= ProductTagRules.MaximumUniqueTagsPerBulkRequest)
            .WithMessage(
                $"A bulk product request can contain at most " +
                $"{ProductTagRules.MaximumUniqueTagsPerBulkRequest} unique tag names.");

        RuleForEach(command => command.Products)
            .ChildRules(product =>
            {
                product.RuleFor(item => item.Title)
                    .NotEmpty()
                    .MaximumLength(250);

                product.RuleFor(item => item.MainSku)
                    .NotEmpty()
                    .MaximumLength(100);

                product.RuleFor(item => item.TypeId)
                    .Must(typeId => !typeId.HasValue || typeId.Value != Guid.Empty)
                    .WithMessage("Product type id cannot be empty.");

                product.RuleFor(item => item.BrandId)
                    .Must(brandId => !brandId.HasValue || brandId.Value != Guid.Empty)
                    .WithMessage("Brand id cannot be empty.");

                product.RuleFor(item => item.TaxRateId)
                    .Must(taxRateId => !taxRateId.HasValue || taxRateId.Value != Guid.Empty)
                    .WithMessage("Tax rate id cannot be empty.");

                product.RuleFor(item => item.Url)
                    .MaximumLength(250);

                product.RuleFor(item => item.Description)
                    .MaximumLength(4000);

                product.RuleFor(item => item.DisplayOrder)
                    .GreaterThanOrEqualTo(0);

                product.RuleFor(item => item.SeoTitle)
                    .MaximumLength(250);

                product.RuleFor(item => item.SeoDescription)
                    .MaximumLength(500);

                product.RuleForEach(item => item.Variants)
                    .ChildRules(variant =>
                    {
                        variant.RuleFor(item => item.Name)
                            .NotEmpty()
                            .MaximumLength(150);

                        variant.RuleFor(item => item.Value)
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
                            .When(item =>
                                item.OpeningUnitCostExcludingVat.HasValue);

                        variant.RuleFor(item => item.OpeningUnitCostIncludingVat)
                            .GreaterThanOrEqualTo(0m)
                            .PrecisionScale(18, 4, false)
                            .When(item =>
                                item.OpeningUnitCostIncludingVat.HasValue);

                        variant.RuleFor(item => item.OpeningUnitCostExcludingVat)
                            .Must((item, cost) =>
                                item.Stock > 0 ||
                                !cost.HasValue ||
                                cost.Value == 0m)
                            .WithMessage(
                                "A positive opening unit cost requires positive opening stock.");

                        variant.RuleFor(item => item.OpeningUnitCostIncludingVat)
                            .Must((item, cost) =>
                                item.Stock > 0 ||
                                !cost.HasValue ||
                                cost.Value == 0m)
                            .WithMessage(
                                "A positive opening unit cost requires positive opening stock.");

                        variant.RuleFor(item => item.CompareAtPrice)
                            .Must((item, compareAtPrice) => !compareAtPrice.HasValue || compareAtPrice.Value >= item.Price)
                            .WithMessage("Compare-at price cannot be lower than price.");

                        variant.RuleFor(item => item.Barcode)
                            .MaximumLength(100);

                        variant.RuleFor(item => item.Material)
                            .MaximumLength(120);
                    });

                product.RuleFor(item => item.Variants)
                    .NotEmpty()
                    .WithMessage("A product must have at least one variant.");

                product.RuleForEach(item => item.Images)
                    .ChildRules(image =>
                    {
                        image.RuleFor(item => item.ImageUrl)
                            .NotEmpty()
                            .MaximumLength(500);

                        image.RuleFor(item => item.DisplayOrder)
                            .GreaterThanOrEqualTo(0);

                        image.RuleFor(item => item.AltText)
                            .MaximumLength(250);
                    });

                product.RuleFor(item => item.Images)
                    .Must(images => images is null || images.Count(image => image.IsMain) <= 1)
                    .WithMessage("A product can contain at most one main image.");

                product.RuleForEach(item => item.CollectionIds)
                    .NotEmpty();

                product.RuleFor(item => item.CollectionIds)
                    .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                    .WithMessage("Collection ids cannot contain duplicates.");

                product.RuleForEach(item => item.TagIds)
                    .NotEmpty();

                product.RuleForEach(item => item.Tags)
                    .NotEmpty()
                    .MaximumLength(ProductTagRules.MaximumTagNameLength);

                product.RuleFor(item => item.Tags)
                    .Must(tags => tags is null || tags.Count <= ProductTagRules.MaximumTagsPerProduct)
                    .WithMessage(
                        $"A product can contain at most {ProductTagRules.MaximumTagsPerProduct} tag names.");
            });
    }
}
