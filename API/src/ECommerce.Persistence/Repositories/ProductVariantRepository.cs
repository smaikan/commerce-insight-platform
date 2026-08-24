using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    // Burada varyant sorgu ve değişiklikleri için aynı istek kapsamındaki DbContext'i hazırlıyorum.
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
            .FirstOrDefaultAsync(
                variant => variant.Id == id && variant.DeletedAtUtc == null && _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null),
                cancellationToken);
    }

    // Burada ürün varyantını güncelleme için takipli şekilde getiriyorum.
    public Task<ProductVariant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductVariants
            .Include(variant => variant.Product)
                .ThenInclude(product => product.TaxRate)
            .Include(variant => variant.OptionValues)
            .FirstOrDefaultAsync(
                variant => variant.Id == id && variant.DeletedAtUtc == null && _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null),
                cancellationToken);
    }

    // Burada aktif ürün varyantını temizlenmiş SKU değeri üzerinden stok güncellemesi için izliyorum.
    public Task<ProductVariant?> GetBySkuForUpdateAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();

        return _context.ProductVariants
            .FirstOrDefaultAsync(
                variant =>
                    variant.Sku == normalizedSku &&
                    variant.DeletedAtUtc == null &&
                    _context.Products.Any(product =>
                        product.Id == variant.ProductId && product.DeletedAtUtc == null),
                cancellationToken);
    }

    // Burada checkout işlemlerinde kilit alma sırasını tutarlı kılmak için varyantları artan kimlikle takipli getiriyorum.
    public async Task<IReadOnlyList<ProductVariant>> GetByIdsForUpdateAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var variantIds = ids.Where(id => id != Guid.Empty).Distinct().OrderBy(id => id).ToList();
        return await _context.ProductVariants
            .Where(variant =>
                variantIds.Contains(variant.Id) &&
                variant.DeletedAtUtc == null &&
                _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null))
            .OrderBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada aktif varyantları temizlenmiş SKU kümesi üzerinden kararlı sırada izliyorum.
    public async Task<IReadOnlyList<ProductVariant>> GetBySkusForUpdateAsync(
        IEnumerable<string> skus,
        CancellationToken cancellationToken = default)
    {
        var normalizedSkus = skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku => sku.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(sku => sku, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return await _context.ProductVariants
            .Where(variant =>
                normalizedSkus.Contains(variant.Sku) &&
                variant.DeletedAtUtc == null &&
                _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null))
            .OrderBy(variant => variant.Sku)
            .ThenBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada batch güncellemesi için ürün vergisi ve seçenek bağlarıyla birlikte varyantları kararlı sırada izliyorum.
    public async Task<IReadOnlyList<ProductVariant>> GetByIdsWithDetailsForUpdateAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var variantIds = ids.Where(id => id != Guid.Empty).Distinct().OrderBy(id => id).ToList();
        return await _context.ProductVariants
            .Include(variant => variant.Product)
                .ThenInclude(product => product.TaxRate)
            .Include(variant => variant.OptionValues)
            .Where(variant =>
                variantIds.Contains(variant.Id) &&
                variant.DeletedAtUtc == null &&
                _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null))
            .OrderBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada bir ürüne ait varyantları SKU değerine göre sıralı getiriyorum.
    public async Task<PagedResult<ProductVariant>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.ProductId == productId &&
                variant.DeletedAtUtc == null &&
                _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null));

        var totalCount = await query.CountAsync(cancellationToken);
        var variants = await query
            .OrderBy(variant => variant.Sku)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductVariant>(variants, pageNumber, pageSize, totalCount);
    }

    // Burada son varyant koruması için ürüne bağlı varyant sayısını okuyorum.
    public Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default) =>
        _context.ProductVariants.CountAsync(
            variant =>
                variant.ProductId == productId &&
                variant.DeletedAtUtc == null &&
                _context.Products.Any(product =>
                    product.Id == variant.ProductId && product.DeletedAtUtc == null),
            cancellationToken);

    // Burada SKU bilgisinin başka bir varyantta kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> SkuExistsAsync(string sku, Guid? excludedVariantId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();

        return _context.ProductVariants.AnyAsync(
            variant =>
                variant.DeletedAtUtc == null &&
                variant.Sku == normalizedSku &&
                (!excludedVariantId.HasValue || variant.Id != excludedVariantId.Value),
            cancellationToken);
    }

    // Burada verilen SKU kümesinin batch dışında kalan mevcut sahiplerini tek sorguda getiriyorum.
    public async Task<IReadOnlyList<string>> GetExistingSkusAsync(
        IEnumerable<string> skus,
        IEnumerable<Guid> excludedVariantIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedSkus = skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku => sku.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var excludedIds = excludedVariantIds.Distinct().ToArray();

        return await _context.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.DeletedAtUtc == null &&
                normalizedSkus.Contains(variant.Sku) &&
                !excludedIds.Contains(variant.Id))
            .Select(variant => variant.Sku)
            .ToListAsync(cancellationToken);
    }
}
