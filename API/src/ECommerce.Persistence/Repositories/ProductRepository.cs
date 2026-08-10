using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    // Burada ürün repository'sini aynı istek kapsamındaki DbContext ile hazırlıyorum.
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
    public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.ProductCollections)
                .ThenInclude(productCollection => productCollection.Collection)
            .Include(product => product.ProductTags)
                .ThenInclude(productTag => productTag.Tag)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                product => product.Id == id && product.DeletedAtUtc == null,
                cancellationToken);
    }

    // Burada verilen ürün id listesindeki ürünleri topluca getiriyorum.
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var productIds = ids
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        return await _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.ProductCollections)
                .ThenInclude(productCollection => productCollection.Collection)
            .Include(product => product.ProductTags)
                .ThenInclude(productTag => productTag.Tag)
            .AsSplitQuery()
            .Where(product => product.DeletedAtUtc == null && productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    // Burada ürünü güncelleme için takipli şekilde getiriyorum.
    public Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .FirstOrDefaultAsync(
                product => product.Id == id && product.DeletedAtUtc == null,
                cancellationToken);
    }

    // Burada checkout işlemlerinde kilit alma sırasını tutarlı kılmak için ürünleri artan kimlikle takipli getiriyorum.
    public async Task<IReadOnlyList<Product>> GetByIdsForUpdateAsync(
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default)
    {
        var productIds = ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToList();
        return await _context.Products
            .Include(product => product.TaxRate)
            .Where(product => product.DeletedAtUtc == null && productIds.Contains(product.Id))
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada vergi oranı güncellemesinden etkilenen ürünleri ve varyantlarını takipli getiriyorum.
    public async Task<IReadOnlyList<Product>> GetByTaxRateIdForUpdateAsync(
        Guid taxRateId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(product => product.Variants)
            .Where(product => product.DeletedAtUtc == null && product.TaxRateId == taxRateId)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada ürünü ilişki değişiklikleri için takipli şekilde getiriyorum.
    public Task<Product?> GetWithRelationsForUpdateAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .Include(product => product.ProductCollections)
                .ThenInclude(productCollection => productCollection.Collection)
            .Include(product => product.ProductTags)
            .Include(product => product.Images)
            .Include(product => product.BundleItems)
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .FirstOrDefaultAsync(
                product => product.Id == id && product.DeletedAtUtc == null,
                cancellationToken);
    }

    // Burada silinmiş ürünleri de kapsayarak ürünü idempotent soft delete için takipli getiriyorum.
    public Task<Product?> GetByIdForDeletionAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    // Burada ürün listesini okuma amaçlı takip etmeden getiriyorum.
    public async Task<PagedResult<Product>> GetListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Where(product => product.DeletedAtUtc == null)
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .Include(product => product.ProductTags)
                .ThenInclude(productTag => productTag.Tag)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            var normalizedMainSkuSearch = search.ToUpperInvariant();
            query = query.Where(product =>
                product.Title.Contains(search) ||
                product.Url.Contains(search) ||
                product.MainSku.Contains(normalizedMainSkuSearch));
        }

        if (filter.TypeId.HasValue)
        {
            query = query.Where(product => product.TypeId == filter.TypeId.Value);
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(product => product.BrandId == filter.BrandId.Value);
        }

        if (filter.CollectionId.HasValue)
        {
            query = query.Where(product => product.ProductCollections.Any(
                relation => relation.CollectionId == filter.CollectionId.Value));
        }

        if (filter.TagId.HasValue)
        {
            query = query.Where(product => product.ProductTags.Any(
                relation => relation.TagId == filter.TagId.Value));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(product => product.Status == filter.Status.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == filter.IsActive.Value);
        }

        if (filter.IsFeatured.HasValue)
        {
            query = query.Where(product => product.IsFeatured == filter.IsFeatured.Value);
        }

        var orderedQuery = filter.SortBy switch
        {
            ProductSortBy.Title => filter.Descending
                ? query.OrderByDescending(product => product.Title)
                : query.OrderBy(product => product.Title),
            ProductSortBy.CreatedAt => filter.Descending
                ? query.OrderByDescending(product => product.CreatedAt)
                : query.OrderBy(product => product.CreatedAt),
            ProductSortBy.PopularityScore => filter.Descending
                ? query.OrderByDescending(product => product.PopularityScore)
                : query.OrderBy(product => product.PopularityScore),
            _ => filter.Descending
                ? query.OrderByDescending(product => product.DisplayOrder).ThenBy(product => product.Title)
                : query.OrderBy(product => product.DisplayOrder).ThenBy(product => product.Title)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await orderedQuery
            .ThenBy(product => product.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    // Burada ürün URL bilgisinin başka bir üründe kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> UrlExistsAsync(string url, long? excludedProductId = null, CancellationToken cancellationToken = default)
    {
        return _context.Products.AnyAsync(
            product =>
                product.DeletedAtUtc == null &&
                product.Url == url &&
                (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
            cancellationToken);
    }

    // Burada yayındaki ürünü güncel veya eski URL değeriyle salt okunur getiriyorum.
    public Task<Product?> GetPublishedByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url.Trim();
        return _context.Products
            .AsNoTracking()
            .Include(product => product.Type)
            .Include(product => product.Brand)
            .Include(product => product.TaxRate)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.ProductCollections)
                .ThenInclude(productCollection => productCollection.Collection)
            .Include(product => product.ProductTags)
                .ThenInclude(productTag => productTag.Tag)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                product => product.DeletedAtUtc == null &&
                    product.IsActive &&
                    product.Status == ProductStatus.Active &&
                    (product.Url == normalizedUrl || product.UrlRedirects.Any(redirect => redirect.Url == normalizedUrl)),
                cancellationToken);
    }

    // Burada yayındaki ürünlerin SEO URL indeksini sayfalı olarak getiriyorum.
    public async Task<PagedResult<ProductSeoIndexItemDto>> GetPublishedSeoIndexAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(product =>
                product.DeletedAtUtc == null &&
                product.IsActive &&
                product.Status == ProductStatus.Active);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductSeoIndexItemDto(
                product.Url,
                product.UpdatedAt ?? product.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSeoIndexItemDto>(items, pageNumber, pageSize, totalCount);
    }

    // Burada URL değerinin eski yönlendirmelerde ayrılmış olup olmadığını kontrol ediyorum.
    public async Task<bool> ReservedUrlExistsAsync(
        string url,
        long? excludedProductId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url.Trim();
        return await _context.ProductUrlRedirects.AnyAsync(
            redirect =>
                redirect.Url == normalizedUrl &&
                (!excludedProductId.HasValue || redirect.ProductId != excludedProductId.Value) &&
                _context.Products.Any(product =>
                    product.Id == redirect.ProductId && product.DeletedAtUtc == null),
            cancellationToken);
    }

    // Burada ürünün eski URL yönlendirmesini veritabanı takibine ekliyorum.
    public async Task AddUrlRedirectAsync(
        ProductUrlRedirect redirect,
        CancellationToken cancellationToken = default)
    {
        await _context.ProductUrlRedirects.AddAsync(redirect, cancellationToken);
    }

    // Burada ana SKU bilgisinin başka bir üründe kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> MainSkuExistsAsync(
        string mainSku,
        long? excludedProductId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedMainSku = mainSku.Trim().ToUpperInvariant();
        return _context.Products.AnyAsync(
            product =>
                product.DeletedAtUtc == null &&
                product.MainSku == normalizedMainSku &&
                (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
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
            .Where(product => product.DeletedAtUtc == null && normalizedUrls.Contains(product.Url))
            .Select(product => product.Url)
            .ToListAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada listedeki ana SKU değerlerinden veritabanında bulunanları getiriyorum.
    public async Task<IReadOnlySet<string>> GetExistingMainSkusAsync(
        IEnumerable<string> mainSkus,
        CancellationToken cancellationToken = default)
    {
        var normalizedMainSkus = mainSkus
            .Where(mainSku => !string.IsNullOrWhiteSpace(mainSku))
            .Select(mainSku => mainSku.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existingMainSkus = await _context.Products
            .AsNoTracking()
            .Where(product => product.DeletedAtUtc == null && normalizedMainSkus.Contains(product.MainSku))
            .Select(product => product.MainSku)
            .ToListAsync(cancellationToken);

        return existingMainSkus.ToHashSet(StringComparer.Ordinal);
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
