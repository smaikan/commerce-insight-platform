using FluentValidation;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(command => command.TypeId)
            .Must(typeId => !typeId.HasValue || typeId.Value != Guid.Empty)
            .WithMessage("Product type id cannot be empty.");

        RuleFor(command => command.BrandId)
            .Must(brandId => !brandId.HasValue || brandId.Value != Guid.Empty)
            .WithMessage("Brand id cannot be empty.");

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
