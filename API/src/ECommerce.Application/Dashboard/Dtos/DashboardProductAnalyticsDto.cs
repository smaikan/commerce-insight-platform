using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Dashboard.Dtos;

public sealed record DashboardProductAnalyticsDto(
    DateOnly From,
    DateOnly To,
    long ClickCount,
    long AddToCartCount,
    long PurchaseCount,
    long FavoriteCount,
    long RatingCount,
    long ReviewCount,
    IReadOnlyList<ProductMetricDto> DailyMetrics,
    IReadOnlyList<DashboardTopProductDto> TopProducts,
    DateTime GeneratedAtUtc);

public sealed record DashboardTopProductDto(
    string ProductId,
    string Title,
    string MainSku,
    long ClickCount,
    long AddToCartCount,
    long PurchaseCount);
