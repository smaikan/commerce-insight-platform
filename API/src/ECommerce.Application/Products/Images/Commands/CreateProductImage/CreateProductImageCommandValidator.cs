using FluentValidation;

namespace ECommerce.Application.Products.Images.Commands.CreateProductImage;

public sealed class CreateProductImageCommandValidator : AbstractValidator<CreateProductImageCommand>
{
    public CreateProductImageCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.ImageUrl)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(command => command.AltText)
            .MaximumLength(250);

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
