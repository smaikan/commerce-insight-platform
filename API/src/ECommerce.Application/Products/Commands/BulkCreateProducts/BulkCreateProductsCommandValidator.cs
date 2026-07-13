using FluentValidation;

namespace ECommerce.Application.Products.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsCommandValidator : AbstractValidator<BulkCreateProductsCommand>
{
    public BulkCreateProductsCommandValidator()
    {
        RuleFor(command => command.Products)
            .NotEmpty()
            .Must(products => products is not null && products.Count <= 500)
            .WithMessage("A bulk product request can contain at most 500 products.");

        RuleForEach(command => command.Products)
            .ChildRules(product =>
            {
                product.RuleFor(item => item.Title)
                    .NotEmpty()
                    .MaximumLength(250);

                product.RuleFor(item => item.TypeId)
                    .NotEmpty();

                product.RuleFor(item => item.BrandId)
                    .Must(brandId => !brandId.HasValue || brandId.Value != Guid.Empty)
                    .WithMessage("Brand id cannot be empty.");

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
                        variant.RuleFor(item => item.Sku)
                            .NotEmpty()
                            .MaximumLength(100);

                        variant.RuleFor(item => item.Price)
                            .GreaterThan(0);

                        variant.RuleFor(item => item.Stock)
                            .GreaterThanOrEqualTo(0);

                        variant.RuleFor(item => item.CompareAtPrice)
                            .Must((item, compareAtPrice) => !compareAtPrice.HasValue || compareAtPrice.Value >= item.Price)
                            .WithMessage("Compare-at price cannot be lower than price.");

                        variant.RuleFor(item => item.Barcode)
                            .MaximumLength(100);

                        variant.RuleFor(item => item.Color)
                            .MaximumLength(80);

                        variant.RuleFor(item => item.Size)
                            .MaximumLength(80);

                        variant.RuleFor(item => item.Material)
                            .MaximumLength(120);
                    });

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
            });
    }
}
