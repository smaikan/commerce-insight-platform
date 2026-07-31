using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductEngagementRepository
{
    Task<FavoriteProduct?> GetFavoriteForUpdateAsync(long productId, long userId, CancellationToken cancellationToken = default);
    Task AddFavoriteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);
    void RemoveFavorite(FavoriteProduct favorite);
    Task<PagedResult<Product>> GetFavoriteProductsAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<ProductRating?> GetRatingForUpdateAsync(long productId, long userId, CancellationToken cancellationToken = default);
    Task<(decimal Sum, long Count)> GetRatingAggregateAsync(long productId, Guid? excludedRatingId, CancellationToken cancellationToken = default);
    Task<bool> HasDeliveredPurchaseAsync(long productId, long userId, CancellationToken cancellationToken = default);
    Task AddRatingAsync(ProductRating rating, CancellationToken cancellationToken = default);

    Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task<ProductReview?> GetReviewForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductReview>> GetReviewsAsync(long productId, bool approvedOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<ProductDailyMetric?> GetProductDailyMetricForUpdateAsync(long productId, DateOnly date, CancellationToken cancellationToken = default);
    // Burada aynı gün için birden çok ürün metriğini tek sorguda takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductDailyMetric>> GetProductDailyMetricsForUpdateAsync(
        IEnumerable<long> productIds,
        DateOnly date,
        CancellationToken cancellationToken = default);
    Task AddProductDailyMetricAsync(ProductDailyMetric metric, CancellationToken cancellationToken = default);
    Task<ProductVariantDailyMetric?> GetVariantDailyMetricForUpdateAsync(Guid variantId, DateOnly date, CancellationToken cancellationToken = default);
    // Burada aynı gün için birden çok varyant metriğini tek sorguda takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductVariantDailyMetric>> GetVariantDailyMetricsForUpdateAsync(
        IEnumerable<Guid> variantIds,
        DateOnly date,
        CancellationToken cancellationToken = default);
    Task AddVariantDailyMetricAsync(ProductVariantDailyMetric metric, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDailyMetric>> GetProductMetricsAsync(long productId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
