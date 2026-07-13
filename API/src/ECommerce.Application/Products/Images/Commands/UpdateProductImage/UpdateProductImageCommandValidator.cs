using FluentValidation;

namespace ECommerce.Application.Products.Images.Commands.UpdateProductImage;

public sealed class UpdateProductImageCommandValidator : AbstractValidator<UpdateProductImageCommand>
{
    public UpdateProductImageCommandValidator()
    {
        RuleFor(command => command.Id)
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
