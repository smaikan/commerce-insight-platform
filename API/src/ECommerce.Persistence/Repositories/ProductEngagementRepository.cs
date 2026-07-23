using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductEngagementRepository : IProductEngagementRepository
{
    private readonly AppDbContext _context;

    // Burada ürün etkileşimi sorguları için veritabanı bağlamını hazırlıyorum.
    public ProductEngagementRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada kullanıcının ürün favorisini güncelleme amacıyla getiriyorum.
    public Task<FavoriteProduct?> GetFavoriteForUpdateAsync(long productId, long userId, CancellationToken cancellationToken = default) =>
        _context.FavoriteProducts.FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId, cancellationToken);

    // Burada yeni favori kaydını veritabanı takibine ekliyorum.
    public async Task AddFavoriteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) =>
        await _context.FavoriteProducts.AddAsync(favorite, cancellationToken);

    // Burada favori kaydını silinmek üzere işaretliyorum.
    public void RemoveFavorite(FavoriteProduct favorite) => _context.FavoriteProducts.Remove(favorite);

    // Burada kullanıcının favori ürünlerini sayfalı şekilde getiriyorum.
    public async Task<PagedResult<Product>> GetFavoriteProductsAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FavoriteProducts.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.Product)
            .Include(product => product.Type)
            .Include(product => product.Brand);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.Title)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Product>(items, pageNumber, pageSize, totalCount);
    }

    // Burada kullanıcının ürün puanını güncelleme amacıyla getiriyorum.
    public Task<ProductRating?> GetRatingForUpdateAsync(long productId, long userId, CancellationToken cancellationToken = default) =>
        _context.ProductRatings.FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId, cancellationToken);

    // Burada ürünün puan toplamını ve uzun tipte puan sayısını hesaplıyorum.
    public async Task<(decimal Sum, long Count)> GetRatingAggregateAsync(long productId, Guid? excludedRatingId, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductRatings.AsNoTracking().Where(item => item.ProductId == productId);
        if (excludedRatingId.HasValue)
        {
            query = query.Where(item => item.Id != excludedRatingId.Value);
        }
        return (await query.SumAsync(item => (decimal)item.RatingValue, cancellationToken), await query.LongCountAsync(cancellationToken));
    }

    // Burada kullanıcının ürünü teslim edilmiş bir siparişte satın aldığını doğruluyorum.
    public Task<bool> HasDeliveredPurchaseAsync(
        long productId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .AsNoTracking()
            .AnyAsync(order =>
                order.UserId == userId &&
                order.Status == ECommerce.Domain.Enums.OrderStatus.Delivered &&
                order.Items.Any(item => item.ProductId == productId),
                cancellationToken);
    }

    // Burada yeni ürün puanını veritabanı takibine ekliyorum.
    public async Task AddRatingAsync(ProductRating rating, CancellationToken cancellationToken = default) =>
        await _context.ProductRatings.AddAsync(rating, cancellationToken);

    // Burada yeni ürün yorumunu veritabanı takibine ekliyorum.
    public async Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default) =>
        await _context.ProductReviews.AddAsync(review, cancellationToken);

    // Burada ürün yorumunu onay güncellemesi için getiriyorum.
    public Task<ProductReview?> GetReviewForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default) =>
        _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);

    // Burada ürün yorumlarını onay durumuna göre sayfalı şekilde getiriyorum.
    public async Task<PagedResult<ProductReview>> GetReviewsAsync(long productId, bool approvedOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductReviews.AsNoTracking().Where(item => item.ProductId == productId);
        if (approvedOnly)
        {
            query = query.Where(item => item.IsApproved);
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<ProductReview>(items, pageNumber, pageSize, totalCount);
    }

    // Burada ürünün günlük metriğini güncelleme amacıyla getiriyorum.
    public Task<ProductDailyMetric?> GetProductDailyMetricForUpdateAsync(long productId, DateOnly date, CancellationToken cancellationToken = default) =>
        _context.ProductDailyMetrics.FirstOrDefaultAsync(item => item.ProductId == productId && item.Date == date, cancellationToken);

    // Burada yeni günlük ürün metriğini veritabanı takibine ekliyorum.
    public async Task AddProductDailyMetricAsync(ProductDailyMetric metric, CancellationToken cancellationToken = default) =>
        await _context.ProductDailyMetrics.AddAsync(metric, cancellationToken);

    // Burada varyantın günlük metriğini güncelleme amacıyla getiriyorum.
    public Task<ProductVariantDailyMetric?> GetVariantDailyMetricForUpdateAsync(Guid variantId, DateOnly date, CancellationToken cancellationToken = default) =>
        _context.ProductVariantDailyMetrics.FirstOrDefaultAsync(item => item.ProductVariantId == variantId && item.Date == date, cancellationToken);

    // Burada yeni günlük varyant metriğini veritabanı takibine ekliyorum.
    public async Task AddVariantDailyMetricAsync(ProductVariantDailyMetric metric, CancellationToken cancellationToken = default) =>
        await _context.ProductVariantDailyMetrics.AddAsync(metric, cancellationToken);

    // Burada ürünün tarih aralığındaki günlük metriklerini getiriyorum.
    public async Task<IReadOnlyList<ProductDailyMetric>> GetProductMetricsAsync(long productId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await _context.ProductDailyMetrics.AsNoTracking()
            .Where(item => item.ProductId == productId && item.Date >= from && item.Date <= to)
            .OrderBy(item => item.Date).ToListAsync(cancellationToken);
}
