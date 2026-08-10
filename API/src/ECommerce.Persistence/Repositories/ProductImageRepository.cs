using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly AppDbContext _context;

    // Burada ürün görseli repository'sini aynı istek kapsamındaki DbContext ile hazırlıyorum.
    public ProductImageRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni ürün görselini veritabanı takibine ekliyorum.
    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ProductImages.AddAsync(image, cancellationToken);
    }

    // Burada takip edilen ürün görselini kalıcı depodan siliyorum.
    public void Remove(ProductImage image) => _context.ProductImages.Remove(image);

    // Burada ürün görselini okuma amaçlı takip etmeden getiriyorum.
    public Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                image => image.Id == id && _context.Products.Any(product =>
                    product.Id == image.ProductId && product.DeletedAtUtc == null),
                cancellationToken);
    }

    // Burada ürün görselini güncelleme için takipli şekilde getiriyorum.
    public Task<ProductImage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .FirstOrDefaultAsync(
                image => image.Id == id && _context.Products.Any(product =>
                    product.Id == image.ProductId && product.DeletedAtUtc == null),
                cancellationToken);
    }

    // Burada silinmiş ürünlere ait kayıtları da kapsayarak görseli bağımsız silme için takipli getiriyorum.
    public Task<ProductImage?> GetByIdForDeletionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .FirstOrDefaultAsync(image => image.Id == id, cancellationToken);
    }

    // Burada ürüne ait diğer ana görseli güncelleme için takipli getiriyorum.
    public Task<ProductImage?> GetMainByProductIdForUpdateAsync(
        long productId,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.ProductImages.FirstOrDefaultAsync(
            image =>
                image.ProductId == productId &&
                image.IsMain &&
                (!excludedImageId.HasValue || image.Id != excludedImageId.Value),
            cancellationToken);
    }

    // Burada yeni görsel eklemeden önce ürünün mevcut görsel sayısını güvenle alıyorum.
    public Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default) =>
        _context.ProductImages.CountAsync(image => image.ProductId == productId, cancellationToken);

    // Burada ana görsel silindiğinde yerine geçecek ilk görseli takipli olarak seçiyorum.
    public Task<ProductImage?> GetFirstByProductIdForUpdateAsync(
        long productId,
        Guid excludedImageId,
        CancellationToken cancellationToken = default) =>
        _context.ProductImages
            .Where(image => image.ProductId == productId && image.Id != excludedImageId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .FirstOrDefaultAsync(cancellationToken);

    // Burada bir ürüne ait görselleri ekrandaki sıralamasına göre getiriyorum.
    public async Task<PagedResult<ProductImage>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductImages
            .AsNoTracking()
            .Where(image =>
                image.ProductId == productId &&
                _context.Products.Any(product =>
                    product.Id == image.ProductId && product.DeletedAtUtc == null));

        var totalCount = await query.CountAsync(cancellationToken);
        var images = await query
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductImage>(images, pageNumber, pageSize, totalCount);
    }
}
