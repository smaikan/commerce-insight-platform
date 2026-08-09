using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard.Dtos;
using ECommerce.Application.Products.Dtos;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada ürün analitiği için yalnız okunur ve veritabanında gruplanan sorguları çalıştırıyorum.
public sealed class ProductAnalyticsReader : IProductAnalyticsReader
{
    private readonly AppDbContext _context;

    // Burada ürün analiz sorgularının veritabanı bağlamını hazırlıyorum.
    public ProductAnalyticsReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada tüm ürünlerin dönem toplamlarını, günlük serisini ve ilk beş ürününü veritabanında hesaplıyorum.
    public async Task<DashboardProductAnalyticsDto> GetDashboardProductAnalyticsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var rangeQuery = _context.ProductDailyMetrics
            .AsNoTracking()
            .Where(metric => metric.Date >= from && metric.Date <= to);
        var totals = await rangeQuery
            .GroupBy(_ => 1)
            .Select(group => new MetricTotals(
                group.Sum(metric => metric.ClickCount),
                group.Sum(metric => metric.AddToCartCount),
                group.Sum(metric => metric.PurchaseCount),
                group.Sum(metric => metric.FavoriteCount),
                group.Sum(metric => metric.RatingCount),
                group.Sum(metric => metric.ReviewCount)))
            .SingleOrDefaultAsync(cancellationToken);
        var populatedDailyMetrics = await rangeQuery
            .GroupBy(metric => metric.Date)
            .OrderBy(group => group.Key)
            .Select(group => new ProductMetricDto(
                group.Key,
                group.Sum(metric => metric.ClickCount),
                group.Sum(metric => metric.AddToCartCount),
                group.Sum(metric => metric.PurchaseCount),
                group.Sum(metric => metric.FavoriteCount),
                group.Sum(metric => metric.RatingCount),
                group.Sum(metric => metric.ReviewCount)))
            .ToListAsync(cancellationToken);
        var topProducts = await rangeQuery
            .GroupBy(metric => new { metric.ProductId, metric.Product.Title, metric.Product.MainSku })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.Title,
                group.Key.MainSku,
                ClickCount = group.Sum(metric => metric.ClickCount),
                AddToCartCount = group.Sum(metric => metric.AddToCartCount),
                PurchaseCount = group.Sum(metric => metric.PurchaseCount)
            })
            .OrderByDescending(product => product.PurchaseCount)
            .ThenByDescending(product => product.AddToCartCount)
            .ThenByDescending(product => product.ClickCount)
            .ThenBy(product => product.ProductId)
            .Take(5)
            .Select(product => new TopProductMetric(
                product.ProductId,
                product.Title,
                product.MainSku,
                product.ClickCount,
                product.AddToCartCount,
                product.PurchaseCount))
            .ToListAsync(cancellationToken);
        var safeTotals = totals ?? new MetricTotals(0, 0, 0, 0, 0, 0);

        return new DashboardProductAnalyticsDto(
            from,
            to,
            safeTotals.ClickCount,
            safeTotals.AddToCartCount,
            safeTotals.PurchaseCount,
            safeTotals.FavoriteCount,
            safeTotals.RatingCount,
            safeTotals.ReviewCount,
            FillMissingDays(from, to, populatedDailyMetrics),
            topProducts.Select(product => new DashboardTopProductDto(
                PublicIdCodec.EncodeProductId(product.ProductId),
                product.Title,
                product.MainSku,
                product.ClickCount,
                product.AddToCartCount,
                product.PurchaseCount)).ToList(),
            DateTime.UtcNow);
    }

    // Burada en fazla doksan günlük sonuçta hareketsiz UTC günlerini sıfır değerlerle tamamlıyorum.
    private static IReadOnlyList<ProductMetricDto> FillMissingDays(
        DateOnly from,
        DateOnly to,
        IReadOnlyList<ProductMetricDto> populatedMetrics)
    {
        var metricsByDate = populatedMetrics.ToDictionary(metric => metric.Date);
        var dayCount = to.DayNumber - from.DayNumber + 1;

        return Enumerable.Range(0, dayCount)
            .Select(offset => metricsByDate.TryGetValue(from.AddDays(offset), out var metric)
                ? metric
                : new ProductMetricDto(from.AddDays(offset), 0, 0, 0, 0, 0, 0))
            .ToList();
    }

    private sealed record MetricTotals(
        long ClickCount,
        long AddToCartCount,
        long PurchaseCount,
        long FavoriteCount,
        long RatingCount,
        long ReviewCount);

    private sealed record TopProductMetric(
        long ProductId,
        string Title,
        string MainSku,
        long ClickCount,
        long AddToCartCount,
        long PurchaseCount);
}
