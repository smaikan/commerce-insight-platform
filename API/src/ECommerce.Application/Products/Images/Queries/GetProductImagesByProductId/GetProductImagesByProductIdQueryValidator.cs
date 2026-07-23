using FluentValidation;

namespace ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;

public sealed class GetProductImagesByProductIdQueryValidator : AbstractValidator<GetProductImagesByProductIdQuery>
{
    public GetProductImagesByProductIdQueryValidator()
    {
        RuleFor(query => query.ProductId)
            .NotEmpty();

        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
