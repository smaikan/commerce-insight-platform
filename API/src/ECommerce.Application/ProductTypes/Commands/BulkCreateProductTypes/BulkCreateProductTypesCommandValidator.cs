using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;

public sealed class BulkCreateProductTypesCommandValidator : AbstractValidator<BulkCreateProductTypesCommand>
{
    public BulkCreateProductTypesCommandValidator()
    {
        RuleFor(command => command.ProductTypes)
            .NotEmpty()
            .Must(productTypes => productTypes is not null && productTypes.Count <= 500)
            .WithMessage("A bulk product type request can contain at most 500 product types.");

        RuleForEach(command => command.ProductTypes)
            .ChildRules(productType =>
            {
                productType.RuleFor(item => item.Name)
                    .NotEmpty()
                    .MaximumLength(150);

                productType.RuleFor(item => item.Description)
                    .MaximumLength(1000);
            });
    }
}
