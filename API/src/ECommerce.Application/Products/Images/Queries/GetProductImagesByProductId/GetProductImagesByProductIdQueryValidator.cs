using FluentValidation;

namespace ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;

public sealed class GetProductImagesByProductIdQueryValidator : AbstractValidator<GetProductImagesByProductIdQuery>
{
    public GetProductImagesByProductIdQueryValidator()
    {
        RuleFor(query => query.ProductId)
            .NotEmpty();
    }
}
