using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.CreateProductType;

public sealed class CreateProductTypeCommandValidator : AbstractValidator<CreateProductTypeCommand>
{
    public CreateProductTypeCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
