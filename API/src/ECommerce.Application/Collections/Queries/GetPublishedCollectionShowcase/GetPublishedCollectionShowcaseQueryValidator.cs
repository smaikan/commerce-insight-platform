using FluentValidation;

namespace ECommerce.Application.Collections.Queries.GetPublishedCollectionShowcase;

public sealed class GetPublishedCollectionShowcaseQueryValidator
    : AbstractValidator<GetPublishedCollectionShowcaseQuery>
{
    // Burada public koleksiyon vitrini sayfalamasını güvenli aralıkta doğruluyorum.
    public GetPublishedCollectionShowcaseQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
