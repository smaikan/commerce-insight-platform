using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.UpdateProductType;

public sealed class UpdateProductTypeCommandValidator : AbstractValidator<UpdateProductTypeCommand>
{
    public UpdateProductTypeCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
