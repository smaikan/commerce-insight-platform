using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    public ProductVariantRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni ürün varyantını veritabanı takibine ekliyorum.
    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariants.AddAsync(variant, cancellationToken);
    }

    // Burada ürün varyantını okuma amaçlı takip etmeden getiriyorum.
    public Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(variant => variant.Id == id, cancellationToken);
    }

    // Burada ürün varyantını güncelleme için takipli şekilde getiriyorum.
    public Task<ProductVariant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductVariants
            .FirstOrDefaultAsync(variant => variant.Id == id, cancellationToken);
    }

    // Burada bir ürüne ait varyantları SKU değerine göre sıralı getiriyorum.
    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.ProductId == productId)
            .OrderBy(variant => variant.Sku)
            .ToListAsync(cancellationToken);
    }

    // Burada SKU bilgisinin başka bir varyantta kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> SkuExistsAsync(string sku, Guid? excludedVariantId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();

        return _context.ProductVariants.AnyAsync(
            variant => variant.Sku == normalizedSku && (!excludedVariantId.HasValue || variant.Id != excludedVariantId.Value),
            cancellationToken);
    }
}
