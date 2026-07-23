using FluentValidation;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypes;

public sealed class GetProductTypesQueryValidator : AbstractValidator<GetProductTypesQuery>
{
    public GetProductTypesQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
