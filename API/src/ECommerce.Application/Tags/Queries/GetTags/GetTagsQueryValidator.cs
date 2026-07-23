using FluentValidation;

namespace ECommerce.Application.Tags.Queries.GetTags;

public sealed class GetTagsQueryValidator : AbstractValidator<GetTagsQuery>
{
    public GetTagsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
