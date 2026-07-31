using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Tags.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductListReader : IProductListReader
{
    private readonly AppDbContext _context;

    // Burada katalog listelemesi için istek kapsamındaki DbContext'i hazırlıyorum.
    public ProductListReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada entity grafiği oluşturmadan liste sözleşmesi için gereken alanları doğrudan seçiyorum.
    public async Task<PagedResult<ProductDto>> GetListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Products.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await CreateOrderedQuery(query, filter)
            .ThenBy(product => product.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .AsSplitQuery()
            .Select(product => new ProductListProjection(
                product.Id,
                product.Title,
                product.MainSku,
                product.Description,
                product.Url,
                product.TypeId,
                product.Type == null ? null : product.Type.Name,
                product.BrandId,
                product.Brand == null ? null : product.Brand.Name,
                product.TaxRateId,
                product.TaxRate == null ? null : product.TaxRate.Name,
                product.TaxRate == null ? null : product.TaxRate.Rate,
                product.Status,
                product.IsActive,
                product.IsFeatured,
                product.DisplayOrder,
                product.SeoTitle,
                product.SeoDescription,
                product.ClickCount,
                product.TotalAddToCartCount,
                product.TotalPurchaseCount,
                product.FavoriteCount,
                product.PopularityScore,
                product.AverageRating,
                product.RatingCount,
                product.ReviewCount,
                product.Variants
                    .OrderBy(variant => variant.Name)
                    .ThenBy(variant => variant.Sku)
                    .Select(variant => new ProductVariantProjection(
                        variant.Id,
                        variant.ProductId,
                        variant.Name,
                        variant.Value,
                        variant.VariantOptionNameId,
                        variant.VariantOptionValueId,
                        variant.Sku,
                        variant.Barcode,
                        variant.Material,
                        variant.Price,
                        variant.NetPrice,
                        variant.CompareAtPrice,
                        variant.Stock,
                        variant.AddToCartCount,
                        variant.PurchaseCount,
                        variant.IsActive))
                    .ToList(),
                product.ProductTags
                    .Where(productTag => productTag.Tag != null)
                    .OrderBy(productTag => productTag.Tag!.Name)
                    .Select(productTag => new TagProjection(
                        productTag.Tag!.Id,
                        productTag.Tag.Name,
                        productTag.Tag.Url,
                        productTag.Tag.IsActive))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(
            items.Select(ToDto).ToList(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    // Burada filtreleri hem toplam sayım hem de sayfalanmış veri sorgusuna aynı biçimde uyguluyorum.
    private static IQueryable<Product> ApplyFilter(IQueryable<Product> query, ProductListFilter filter)
    {
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

        return query;
    }

    // Burada mevcut endpoint sıralama kurallarını sorguya koruyarak uyguluyorum.
    private static IOrderedQueryable<Product> CreateOrderedQuery(
        IQueryable<Product> query,
        ProductListFilter filter)
    {
        return filter.SortBy switch
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
    }

    // Burada sorgu projeksiyonunu mevcut ProductDto sözleşmesine dönüştürüyorum.
    private static ProductDto ToDto(ProductListProjection product)
    {
        return new ProductDto(
            PublicIdCodec.EncodeProductId(product.Id), product.Title, product.MainSku,
            product.Description, product.Url, product.TypeId, product.TypeName, product.BrandId,
            product.BrandName, product.TaxRateId, product.TaxRateName, product.TaxRatePercentage,
            product.Status, product.IsActive, product.IsFeatured, product.Variants.Count > 0,
            product.DisplayOrder, product.SeoTitle, product.SeoDescription, product.ClickCount,
            product.TotalAddToCartCount, product.TotalPurchaseCount, product.FavoriteCount,
            product.PopularityScore, product.AverageRating, product.RatingCount, product.ReviewCount,
            product.Variants.Select(variant => new ProductVariantDto(
                variant.Id, PublicIdCodec.EncodeProductId(variant.ProductId), variant.Name, variant.Value,
                variant.VariantOptionNameId, variant.VariantOptionValueId, variant.Sku,
                variant.Barcode, variant.Material, variant.Price, variant.NetPrice, variant.CompareAtPrice,
                variant.Stock, variant.AddToCartCount, variant.PurchaseCount, variant.IsActive)).ToList(),
            product.Tags.Select(tag => new TagDto(tag.Id, tag.Name, tag.Url, tag.IsActive)).ToList());
    }

    private sealed record ProductListProjection(long Id, string Title, string MainSku, string? Description,
        string Url, Guid? TypeId, string? TypeName, Guid? BrandId, string? BrandName, Guid? TaxRateId,
        string? TaxRateName, decimal? TaxRatePercentage, ECommerce.Domain.Enums.ProductStatus Status,
        bool IsActive, bool IsFeatured, int DisplayOrder, string? SeoTitle, string? SeoDescription,
        long ClickCount, long TotalAddToCartCount, long TotalPurchaseCount, long FavoriteCount,
        long PopularityScore, decimal AverageRating, long RatingCount, long ReviewCount,
        IReadOnlyList<ProductVariantProjection> Variants, IReadOnlyList<TagProjection> Tags);

    private sealed record ProductVariantProjection(Guid Id, long ProductId, string Name, string Value,
        Guid? VariantOptionNameId, Guid? VariantOptionValueId, string Sku,
        string? Barcode, string? Material, decimal Price, decimal NetPrice, decimal? CompareAtPrice,
        int Stock, long AddToCartCount, long PurchaseCount, bool IsActive);

    private sealed record TagProjection(Guid Id, string Name, string Url, bool IsActive);
}
