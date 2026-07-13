using FluentValidation;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(command =>  command.TypeId)
            .NotEmpty();

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
    }
}
