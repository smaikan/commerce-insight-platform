using FluentValidation;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;

public sealed class GetProductVariantsByProductIdQueryValidator : AbstractValidator<GetProductVariantsByProductIdQuery>
{
    public GetProductVariantsByProductIdQueryValidator()
    {
        RuleFor(query => query.ProductId)
            .NotEmpty();
    }
}
