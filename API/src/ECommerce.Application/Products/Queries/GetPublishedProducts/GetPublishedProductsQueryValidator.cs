using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetPublishedProducts;

public sealed class GetPublishedProductsQueryValidator : AbstractValidator<GetPublishedProductsQuery>
{
    // Burada storefront sayfalama ve sıralama değerlerini güvenli aralıkta doğruluyorum.
    public GetPublishedProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SortBy).IsInEnum();
    }
}
