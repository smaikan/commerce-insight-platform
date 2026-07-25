using ECommerce.Application.Common.Services;
using FluentValidation;

namespace ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;

public sealed class UpdateProductRelationsCommandValidator : AbstractValidator<UpdateProductRelationsCommand>
{
    // Burada ürün ilişki isteğinin koleksiyon, etiket ve bundle kurallarını doğruluyorum.
    public UpdateProductRelationsCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CollectionIds).NotNull()
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Collection ids must be unique.");
        RuleForEach(command => command.CollectionIds).NotEmpty();
        RuleFor(command => command.TagIds).NotNull()
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Tag ids must be unique.");
        RuleForEach(command => command.TagIds).NotEmpty();
        RuleForEach(command => command.Tags)
            .NotEmpty()
            .MaximumLength(ProductTagRules.MaximumTagNameLength);
        RuleFor(command => command.Tags)
            .Must(tags => tags is null || tags.Count <= ProductTagRules.MaximumTagsPerProduct)
            .WithMessage($"A product can contain at most {ProductTagRules.MaximumTagsPerProduct} tag names.");
        RuleFor(command => command.BundleItems).NotNull()
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Bundle product ids must be unique.");
        RuleForEach(command => command.BundleItems).ChildRules(item =>
        {
            item.RuleFor(value => value.ProductId).NotEmpty();
            item.RuleFor(value => value.Quantity).GreaterThan(0);
        });
    }
}
