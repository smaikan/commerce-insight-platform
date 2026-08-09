using ECommerce.Application.Dashboard.Queries.GetDashboardProductAnalytics;
using ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;
using FluentAssertions;

namespace ECommerce.UnitTests.Application;

public sealed class ProductAnalyticsQueryValidatorTests
{
    // Burada başlangıcı bitişten sonra olan ürün metriği sorgusunun reddedildiğini doğruluyorum.
    [Fact]
    public void Product_Metrics_Should_Reject_Reversed_Date_Range()
    {
        var result = new GetProductMetricsQueryValidator().Validate(
            new GetProductMetricsQuery(1, new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("sonra"));
    }

    // Burada doksan günü aşan dashboard ürün analizi sorgusunun reddedildiğini doğruluyorum.
    [Fact]
    public void Dashboard_Product_Analytics_Should_Reject_Range_Longer_Than_Ninety_Days()
    {
        var result = new GetDashboardProductAnalyticsQueryValidator().Validate(
            new GetDashboardProductAnalyticsQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("90"));
    }
}
