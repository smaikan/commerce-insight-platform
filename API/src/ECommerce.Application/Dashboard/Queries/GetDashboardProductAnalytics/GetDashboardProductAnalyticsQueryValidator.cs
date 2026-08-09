using ECommerce.Application.Products.Engagement.Queries;
using FluentValidation;

namespace ECommerce.Application.Dashboard.Queries.GetDashboardProductAnalytics;

public sealed class GetDashboardProductAnalyticsQueryValidator : AbstractValidator<GetDashboardProductAnalyticsQuery>
{
    // Burada dashboard ürün analizi için ortak tarih aralığı kurallarını uyguluyorum.
    public GetDashboardProductAnalyticsQueryValidator()
    {
        ProductAnalyticsDateRangeRules.Apply(this, query => query.From, query => query.To);
    }
}
