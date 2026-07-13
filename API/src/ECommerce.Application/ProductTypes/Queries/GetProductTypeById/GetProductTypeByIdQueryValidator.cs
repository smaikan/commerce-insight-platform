using FluentValidation;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypeById;

public sealed class GetProductTypeByIdQueryValidator : AbstractValidator<GetProductTypeByIdQuery>
{
    public GetProductTypeByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
