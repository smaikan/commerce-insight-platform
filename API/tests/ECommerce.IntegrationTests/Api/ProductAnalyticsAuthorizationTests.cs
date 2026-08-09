using ECommerce.API.Controllers.Dashboard;
using ECommerce.API.Controllers.Product;
using ECommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.IntegrationTests.Api;

public sealed class ProductAnalyticsAuthorizationTests
{
    // Burada ürün analitiği endpointlerinin AdminOnly politika metadata'sıyla korunduğunu doğruluyorum.
    [Fact]
    public void Analytics_Endpoints_Should_Require_AdminOnly_Policy()
    {
        var productMetricsAuthorization = typeof(ProductEngagementController)
            .GetMethod(nameof(ProductEngagementController.GetMetrics))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();
        var dashboardAuthorization = typeof(DashboardController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        productMetricsAuthorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
        dashboardAuthorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
    }
}
