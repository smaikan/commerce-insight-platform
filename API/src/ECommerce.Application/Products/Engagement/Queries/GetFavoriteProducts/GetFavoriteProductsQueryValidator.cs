using FluentValidation;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryValidator : AbstractValidator<GetFavoriteProductsQuery>
{
    // Burada sayfalama sınırları ile varsa guest session uzunluğunu doğruluyorum.
    public GetFavoriteProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SessionId)
            .MaximumLength(FavoriteProduct.MaximumSessionIdLength);
    }
}
