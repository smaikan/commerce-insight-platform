using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni ürünü veritabanı takibine ekliyorum.
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    // Burada birden fazla ürünü tek seferde veritabanı takibine ekliyorum.
    public async Task AddRangeAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddRangeAsync(products, cancellationToken);
    }

    // Burada ürünü detay okumak için takip etmeden getiriyorum.
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    // Burada verilen ürün id listesindeki ürünleri topluca getiriyorum.
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var productIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return await _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    // Burada ürünü güncelleme için takipli şekilde getiriyorum.
    public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    // Burada ürün listesini okuma amaçlı takip etmeden getiriyorum.
    public async Task<IReadOnlyList<Product>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Title)
            .ToListAsync(cancellationToken);
    }

    // Burada ürün URL bilgisinin başka bir üründe kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> UrlExistsAsync(string url, Guid? excludedProductId = null, CancellationToken cancellationToken = default)
    {
        return _context.Products.AnyAsync(
            product => product.Url == url && (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
            cancellationToken);
    }

    // Burada listedeki URL değerlerinden veritabanında olanları buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var normalizedUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingUrls = await _context.Products
            .AsNoTracking()
            .Where(product => normalizedUrls.Contains(product.Url))
            .Select(product => product.Url)
            .ToListAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada listedeki varyant SKU değerlerinden veritabanında olanları buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingVariantSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        var normalizedSkus = skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku => sku.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingSkus = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant => normalizedSkus.Contains(variant.Sku))
            .Select(variant => variant.Sku)
            .ToListAsync(cancellationToken);

        return existingSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
