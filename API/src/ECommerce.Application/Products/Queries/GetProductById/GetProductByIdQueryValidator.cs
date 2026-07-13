using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
