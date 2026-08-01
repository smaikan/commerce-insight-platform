using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetProductSeoIndex;

public sealed class GetProductSeoIndexQueryValidator : AbstractValidator<GetProductSeoIndexQuery>
{
    public GetProductSeoIndexQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
