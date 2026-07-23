using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;

public sealed class GetProductMetricsQueryValidator : AbstractValidator<GetProductMetricsQuery>
{
    public GetProductMetricsQueryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From);
    }
}
