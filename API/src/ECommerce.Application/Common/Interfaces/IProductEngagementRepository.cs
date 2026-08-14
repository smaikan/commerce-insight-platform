using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductEngagementRepository
{
    // Burada owner kapsamındaki tek favoriyi takipli getirme sözleşmesini tanımlıyorum.
    Task<FavoriteProduct?> GetFavoriteForUpdateAsync(long productId, FavoriteOwner owner, CancellationToken cancellationToken = default);
    // Burada yeni favoriyi takibe ekleme sözleşmesini tanımlıyorum.
    Task AddFavoriteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default);
    // Burada favoriyi silinmek üzere işaretleme sözleşmesini tanımlıyorum.
    void RemoveFavorite(FavoriteProduct favorite);
    // Burada owner'ın favori sayısını hesaplama sözleşmesini tanımlıyorum.
    Task<int> CountFavoritesAsync(FavoriteOwner owner, CancellationToken cancellationToken = default);
    // Burada owner favorilerini claim için takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<FavoriteProduct>> GetFavoritesForUpdateAsync(FavoriteOwner owner, CancellationToken cancellationToken = default);
    // Burada owner favori ürünlerini sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Product>> GetFavoriteProductsAsync(FavoriteOwner owner, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Burada kullanıcı puanını takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductRating?> GetRatingForUpdateAsync(long productId, long userId, CancellationToken cancellationToken = default);
    // Burada ürün puan toplamı ve sayısını hesaplama sözleşmesini tanımlıyorum.
    Task<(decimal Sum, long Count)> GetRatingAggregateAsync(long productId, Guid? excludedRatingId, CancellationToken cancellationToken = default);
    // Burada kullanıcının teslim edilmiş satın alımını doğrulama sözleşmesini tanımlıyorum.
    Task<bool> HasDeliveredPurchaseAsync(long productId, long userId, CancellationToken cancellationToken = default);
    // Burada yeni puanı takibe ekleme sözleşmesini tanımlıyorum.
    Task AddRatingAsync(ProductRating rating, CancellationToken cancellationToken = default);

    // Burada yeni ürün yorumunu takibe ekleme sözleşmesini tanımlıyorum.
    Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default);
    // Burada ürün yorumunu takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductReview?> GetReviewForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default);
    // Burada ürün yorumlarını sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<ProductReview>> GetReviewsAsync(long productId, bool approvedOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Burada ürün günlük metriğini takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductDailyMetric?> GetProductDailyMetricForUpdateAsync(long productId, DateOnly date, CancellationToken cancellationToken = default);
    // Burada aynı gün için birden çok ürün metriğini tek sorguda takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductDailyMetric>> GetProductDailyMetricsForUpdateAsync(
        IEnumerable<long> productIds,
        DateOnly date,
        CancellationToken cancellationToken = default);
    // Burada yeni ürün günlük metriğini takibe ekleme sözleşmesini tanımlıyorum.
    Task AddProductDailyMetricAsync(ProductDailyMetric metric, CancellationToken cancellationToken = default);
    // Burada varyant günlük metriğini takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductVariantDailyMetric?> GetVariantDailyMetricForUpdateAsync(Guid variantId, DateOnly date, CancellationToken cancellationToken = default);
    // Burada aynı gün için birden çok varyant metriğini tek sorguda takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductVariantDailyMetric>> GetVariantDailyMetricsForUpdateAsync(
        IEnumerable<Guid> variantIds,
        DateOnly date,
        CancellationToken cancellationToken = default);
    // Burada yeni varyant günlük metriğini takibe ekleme sözleşmesini tanımlıyorum.
    Task AddVariantDailyMetricAsync(ProductVariantDailyMetric metric, CancellationToken cancellationToken = default);
    // Burada ürünün tarih aralığındaki metriklerini getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductDailyMetric>> GetProductMetricsAsync(long productId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
