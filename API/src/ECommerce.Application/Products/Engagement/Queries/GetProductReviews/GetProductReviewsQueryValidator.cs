using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductReviews;

public sealed class GetProductReviewsQueryValidator : AbstractValidator<GetProductReviewsQuery>
{
    public GetProductReviewsQueryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
