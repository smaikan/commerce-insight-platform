using ECommerce.Application.Common.Services;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    // Burada ürün güncelleme isteğinin temel alan kurallarını tanımlıyorum.
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(command => command.MainSku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Type)
            .MaximumLength(150);

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

        RuleForEach(command => command.Tags)
            .NotEmpty()
            .MaximumLength(ProductTagRules.MaximumTagNameLength);

        RuleFor(command => command.Tags)
            .Must(tags => tags is null || tags.Count <= ProductTagRules.MaximumTagsPerProduct)
            .WithMessage($"A product can contain at most {ProductTagRules.MaximumTagsPerProduct} tags.");
    }
}
