using ECommerce.Application.Dashboard.Dtos;
using ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ProductAnalyticsPersistenceTests
{
    // Burada tek ürün serisinin boş UTC günleri doldurduğunu ve başka ürün metriğini karıştırmadığını doğruluyorum.
    [Fact]
    public async Task Product_Metrics_Should_Fill_Missing_Days_Without_Mixing_Products()
    {
        await using var fixture = await ProductAnalyticsFixture.CreateAsync();
        var firstMetric = CreateMetric(fixture.FirstProduct.Id, new DateOnly(2026, 8, 1), clicks: 4);
        var otherProductMetric = CreateMetric(fixture.SecondProduct.Id, new DateOnly(2026, 8, 1), clicks: 99);
        fixture.Context.ProductDailyMetrics.AddRange(firstMetric, otherProductMetric);
        await fixture.Context.SaveChangesAsync();
        var handler = new GetProductMetricsQueryHandler(new ProductEngagementRepository(fixture.Context));

        var metrics = await handler.Handle(
            new GetProductMetricsQuery(fixture.FirstProduct.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3)),
            CancellationToken.None);

        metrics.Should().HaveCount(3);
        metrics.Select(metric => metric.Date).Should().ContainInOrder(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 3));
        metrics[0].ClickCount.Should().Be(4);
        metrics[1].ClickCount.Should().Be(0);
        metrics[2].ClickCount.Should().Be(0);
    }

    // Burada dashboard toplamlarının günlük metriklerden geldiğini ve ilk beş ürünün doğru sıralandığını doğruluyorum.
    [Fact]
    public async Task Dashboard_Product_Analytics_Should_Aggregate_Daily_Metrics_And_Return_Top_Five()
    {
        await using var fixture = await ProductAnalyticsFixture.CreateAsync(6);
        var dayOne = new DateOnly(2026, 8, 1);
        fixture.Context.ProductDailyMetrics.AddRange(
            CreateMetric(fixture.Products[0].Id, dayOne, clicks: 10, carts: 3, purchases: 2, favorites: 2, ratings: 1, reviews: 1),
            CreateMetric(fixture.Products[1].Id, dayOne, clicks: 30, carts: 5, purchases: 2, favorites: 1, ratings: 2, reviews: 1),
            CreateMetric(fixture.Products[2].Id, dayOne, clicks: 60, carts: 8, purchases: 3, favorites: 3, ratings: 1),
            CreateMetric(fixture.Products[3].Id, dayOne, clicks: 50, carts: 9, purchases: 3, favorites: 4, reviews: 2),
            CreateMetric(fixture.Products[4].Id, dayOne, clicks: 100, carts: 1, purchases: 1, favorites: 5, ratings: 1, reviews: 1),
            CreateMetric(fixture.Products[5].Id, dayOne, clicks: 5, carts: 1, favorites: 6),
            CreateMetric(fixture.Products[0].Id, dayOne.AddDays(1), clicks: 7, carts: 2, purchases: 1, favorites: 7, ratings: 1, reviews: 2));
        await fixture.Context.SaveChangesAsync();
        var reader = new ProductAnalyticsReader(fixture.Context);

        var analytics = await reader.GetDashboardProductAnalyticsAsync(dayOne, dayOne.AddDays(2));

        analytics.ClickCount.Should().Be(262);
        analytics.AddToCartCount.Should().Be(29);
        analytics.PurchaseCount.Should().Be(12);
        analytics.FavoriteCount.Should().Be(28);
        analytics.RatingCount.Should().Be(6);
        analytics.ReviewCount.Should().Be(7);
        analytics.DailyMetrics.Should().HaveCount(3);
        analytics.DailyMetrics.Sum(metric => metric.ClickCount).Should().Be(analytics.ClickCount);
        analytics.DailyMetrics.Sum(metric => metric.AddToCartCount).Should().Be(analytics.AddToCartCount);
        analytics.DailyMetrics.Sum(metric => metric.PurchaseCount).Should().Be(analytics.PurchaseCount);
        analytics.DailyMetrics.Sum(metric => metric.FavoriteCount).Should().Be(analytics.FavoriteCount);
        analytics.DailyMetrics.Sum(metric => metric.RatingCount).Should().Be(analytics.RatingCount);
        analytics.DailyMetrics.Sum(metric => metric.ReviewCount).Should().Be(analytics.ReviewCount);
        analytics.DailyMetrics[2].ClickCount.Should().Be(0);
        analytics.TopProducts.Should().HaveCount(5);
        analytics.TopProducts.Select(product => product.Title).Should().ContainInOrder(
            fixture.Products[3].Title,
            fixture.Products[2].Title,
            fixture.Products[0].Title,
            fixture.Products[1].Title,
            fixture.Products[4].Title);
        analytics.TopProducts.Should().OnlyContain(product => product.ProductId.StartsWith("P"));
    }

    // Burada sayısal günlük metrik kaydını test verisinden oluşturuyorum.
    private static ProductDailyMetric CreateMetric(
        long productId,
        DateOnly date,
        int clicks = 0,
        int carts = 0,
        int purchases = 0,
        int favorites = 0,
        int ratings = 0,
        int reviews = 0)
    {
        var metric = new ProductDailyMetric(productId, date);
        for (var index = 0; index < clicks; index++) metric.IncreaseClickCount();
        if (carts > 0) metric.IncreaseAddToCartCount(carts);
        if (purchases > 0) metric.IncreasePurchaseCount(purchases);
        for (var index = 0; index < favorites; index++) metric.IncreaseFavoriteCount();
        for (var index = 0; index < ratings; index++) metric.IncreaseRatingCount();
        for (var index = 0; index < reviews; index++) metric.IncreaseReviewCount();
        return metric;
    }

    private sealed class ProductAnalyticsFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public IReadOnlyList<Product> Products { get; }
        public Product FirstProduct => Products[0];
        public Product SecondProduct => Products[1];

        // Burada analitik sorguları için açık SQLite bağlantısını ve ürün verisini saklıyorum.
        private ProductAnalyticsFixture(SqliteConnection connection, AppDbContext context, IReadOnlyList<Product> products)
        {
            _connection = connection;
            Context = context;
            Products = products;
        }

        // Burada birbirinden bağımsız ürün metrik test bağlamını oluşturuyorum.
        public static async Task<ProductAnalyticsFixture> CreateAsync(int productCount = 2)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            var products = Enumerable.Range(1, productCount)
                .Select(index => new Product($"Ürün {index}", $"urun-{index}", $"SKU-{index:D3}"))
                .ToList();
            context.Products.AddRange(products);
            await context.SaveChangesAsync();
            return new ProductAnalyticsFixture(connection, context, products);
        }

        // Burada test bağlamını ve açık bellek içi bağlantıyı birlikte kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
