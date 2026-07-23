using FluentValidation;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;

public sealed class GetProductVariantsByProductIdQueryValidator : AbstractValidator<GetProductVariantsByProductIdQuery>
{
    public GetProductVariantsByProductIdQueryValidator()
    {
        RuleFor(query => query.ProductId)
            .NotEmpty();

        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
