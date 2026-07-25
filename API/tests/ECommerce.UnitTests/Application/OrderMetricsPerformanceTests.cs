using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class OrderMetricsPerformanceTests
{
    // Burada yüz checkout satırının günlük metriklerini satır başına sorgu yerine iki toplu okuma ile işlediğini doğruluyorum.
    [Fact]
    public async Task RecordPurchasedQuantities_Should_Use_Batch_Metric_Lookups_For_One_Hundred_Lines()
    {
        var lines = Enumerable.Range(1, 100)
            .Select(index => CreateLine(index))
            .ToList();
        var engagement = new Mock<IProductEngagementRepository>();
        engagement.Setup(repository => repository.GetProductDailyMetricsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                new DateOnly(2026, 7, 24),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        engagement.Setup(repository => repository.GetVariantDailyMetricsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                new DateOnly(2026, 7, 24),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        engagement.Setup(repository => repository.AddProductDailyMetricAsync(
                It.IsAny<ProductDailyMetric>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        engagement.Setup(repository => repository.AddVariantDailyMetricAsync(
                It.IsAny<ProductVariantDailyMetric>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var recorder = new OrderMetricsRecorder(engagement.Object, new FixedClock());

        await recorder.RecordPurchasedQuantitiesAsync(lines, CancellationToken.None);

        engagement.Verify(repository => repository.GetProductDailyMetricsForUpdateAsync(
            It.IsAny<IEnumerable<long>>(),
            It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Once);
        engagement.Verify(repository => repository.GetVariantDailyMetricsForUpdateAsync(
            It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Once);
        engagement.Verify(repository => repository.GetProductDailyMetricForUpdateAsync(
            It.IsAny<long>(),
            It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Never);
        engagement.Verify(repository => repository.GetVariantDailyMetricForUpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Never);
        lines.Select(line => line.Product.TotalPurchaseCount).Should().OnlyContain(count => count == 1);
        lines.Select(line => line.Variant.PurchaseCount).Should().OnlyContain(count => count == 1);
    }

    // Burada performans testinin her satırı için benzersiz ürün ve varyant içeren güvenilir metrik girdisi oluşturuyorum.
    private static PurchaseMetricLine CreateLine(int index)
    {
        var product = new Product(
                $"Product {index}",
                $"metric-product-{index}",
                $"METRIC-{index}",
                status: ProductStatus.Active)
            .WithId(index);
        var variant = new ProductVariant(
            product.Id,
            "Default",
            $"METRIC-SKU-{index}",
            10m,
            10);
        return new PurchaseMetricLine(product, variant, 1);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
    }
}
