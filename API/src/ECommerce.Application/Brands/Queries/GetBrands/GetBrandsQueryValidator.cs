using FluentValidation;

namespace ECommerce.Application.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryValidator : AbstractValidator<GetBrandsQuery>
{
    public GetBrandsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
