using FluentValidation;

namespace ECommerce.Application.ProductTypes.Queries.GetPublishedProductTypeShowcase;

public sealed class GetPublishedProductTypeShowcaseQueryValidator
    : AbstractValidator<GetPublishedProductTypeShowcaseQuery>
{
    // Burada kategori vitrini sayfalamasını güvenli sınırlar içinde tutuyorum.
    public GetPublishedProductTypeShowcaseQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
