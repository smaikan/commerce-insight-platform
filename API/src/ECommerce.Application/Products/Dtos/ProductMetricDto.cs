using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductMetricDto(
    DateOnly Date,
    long ClickCount,
    long AddToCartCount,
    long PurchaseCount,
    long FavoriteCount,
    long RatingCount,
    long ReviewCount);

public static class ProductMetricDtoMapping
{
    // Burada günlük ürün metriğini API DTO'suna dönüştürüyorum.
    public static ProductMetricDto ToDto(this ProductDailyMetric metric) => new(
        metric.Date, metric.ClickCount, metric.AddToCartCount, metric.PurchaseCount,
        metric.FavoriteCount, metric.RatingCount, metric.ReviewCount);
}
