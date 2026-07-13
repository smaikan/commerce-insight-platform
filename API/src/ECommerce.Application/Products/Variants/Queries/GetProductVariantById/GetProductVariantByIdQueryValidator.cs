using FluentValidation;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantById;

public sealed class GetProductVariantByIdQueryValidator : AbstractValidator<GetProductVariantByIdQuery>
{
    public GetProductVariantByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
