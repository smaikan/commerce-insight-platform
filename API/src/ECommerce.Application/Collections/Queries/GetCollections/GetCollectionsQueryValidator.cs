using FluentValidation;

namespace ECommerce.Application.Collections.Queries.GetCollections;

public sealed class GetCollectionsQueryValidator : AbstractValidator<GetCollectionsQuery>
{
    public GetCollectionsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
