using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests.Api;

public sealed class ProductAnalyticsUnauthorizedHttpTests
{
    // Burada anonim HTTP isteklerinin iki ürün analitiği endpointinde de reddedildiğini doğruluyorum.
    [Theory]
    [InlineData("/api/product-engagement/products/P00001/metrics?from=2026-08-01&to=2026-08-07")]
    [InlineData("/api/dashboard/product-analytics?from=2026-08-01&to=2026-08-07")]
    public async Task Analytics_Endpoints_Should_Reject_Anonymous_Requests(string path)
    {
        await using var factory = new ProductAnalyticsApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class ProductAnalyticsApiFactory : WebApplicationFactory<Program>
    {
        // Burada anonim analiz HTTP testi için API ortam ayarlarını izole ediyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=(localdb)\\mssqllocaldb;Database=ECommerceProductAnalyticsHttpTests;Trusted_Connection=True;");
            builder.UseSetting("Jwt:Issuer", "ECommerce.IntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.IntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.ProductAnalyticsIntegrationTests.DataProtection"));
        }
    }
}
