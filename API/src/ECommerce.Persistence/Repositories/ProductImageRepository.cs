using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly AppDbContext _context;

    public ProductImageRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni ürün görselini veritabanı takibine ekliyorum.
    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ProductImages.AddAsync(image, cancellationToken);
    }

    // Burada ürün görselini okuma amaçlı takip etmeden getiriyorum.
    public Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == id, cancellationToken);
    }

    // Burada ürün görselini güncelleme için takipli şekilde getiriyorum.
    public Task<ProductImage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .FirstOrDefaultAsync(image => image.Id == id, cancellationToken);
    }

    // Burada bir ürüne ait görselleri ekrandaki sıralamasına göre getiriyorum.
    public async Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .AsNoTracking()
            .Where(image => image.ProductId == productId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .ToListAsync(cancellationToken);
    }
}
