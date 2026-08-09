using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;

public sealed class GetProductMetricsQueryValidator : AbstractValidator<GetProductMetricsQuery>
{
    // Burada ürün metriği tarih aralığının zorunlu iş kurallarını uyguluyorum.
    public GetProductMetricsQueryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
        ProductAnalyticsDateRangeRules.Apply(this, query => query.From, query => query.To);
    }
}
