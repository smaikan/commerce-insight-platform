using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.ReplaceProductPerformanceMetrics;

public sealed record ReplaceProductPerformanceMetricsCommand(
    IReadOnlyList<ProductPerformanceMetricsItem> Items) : IRequest<IReadOnlyList<ProductDto>>;

public sealed record ProductPerformanceMetricsItem(
    long ProductId,
    long ClickCount,
    long TotalAddToCartCount,
    long TotalPurchaseCount,
    long FavoriteCount,
    decimal AverageRating,
    long RatingCount,
    long ReviewCount);
