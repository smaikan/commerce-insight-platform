using FluentValidation;

namespace ECommerce.Application.Brands.Queries.GetBrandById;

public sealed class GetBrandByIdQueryValidator : AbstractValidator<GetBrandByIdQuery>
{
    public GetBrandByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
