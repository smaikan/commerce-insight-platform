using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryValidator : AbstractValidator<GetFavoriteProductsQuery>
{
    public GetFavoriteProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
