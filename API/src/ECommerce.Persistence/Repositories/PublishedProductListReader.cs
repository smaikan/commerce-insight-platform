using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada storefront için yalnızca satışa açık ürün kartlarını doğrudan projekte ediyorum.
public sealed class PublishedProductListReader : IPublishedProductListReader
{
    private readonly AppDbContext _context;

    // Burada yayımlanmış ürün sorguları için DbContext'i hazırlıyorum.
    public PublishedProductListReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada aktif ürünleri storefront sözleşmesine uygun, sayfalı ve sıralı olarak getiriyorum.
    public async Task<PagedResult<PublishedProductListItemDto>> GetListAsync(
        PublishedProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(product =>
                product.DeletedAtUtc == null &&
                product.IsActive &&
                product.Status == ProductStatus.Active);
        query = ApplyFilter(query, filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await CreateOrderedQuery(query, filter)
            .ThenBy(product => product.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(product => new PublishedProductProjection(
                product.Id,
                product.Title,
                product.Url,
                product.Description,
                product.Brand == null ? null : product.Brand.Name,
                product.AverageRating,
                product.RatingCount,
                product.Variants
                    .Where(variant => variant.IsActive)
                    .OrderBy(variant => variant.Price)
                    .ThenBy(variant => variant.Id)
                    .Select(variant => new ProductPriceProjection(variant.Price, variant.CompareAtPrice))
                    .FirstOrDefault(),
                product.Images
                    .OrderByDescending(image => image.IsMain)
                    .ThenBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => new ProductImageProjection(
                        image.Id,
                        image.ImageUrl,
                        image.AltText,
                        image.DisplayOrder,
                        image.IsMain))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublishedProductListItemDto>(
            items.Select(ToDto).ToList(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    // Burada storefront sınıflandırma filtrelerini veritabanı sorgusuna birlikte uyguluyorum.
    private static IQueryable<Product> ApplyFilter(
        IQueryable<Product> query,
        PublishedProductListFilter filter)
    {
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

        return query;
    }

    // Burada storefront sıralama seçeneğini güvenli ve kararlı EF sorgusuna çeviriyorum.
    private static IOrderedQueryable<Product> CreateOrderedQuery(
        IQueryable<Product> query,
        PublishedProductListFilter filter) =>
        filter.SortBy switch
        {
            PublishedProductSortBy.Popularity => filter.Descending
                ? query.OrderByDescending(product => product.PopularityScore)
                : query.OrderBy(product => product.PopularityScore),
            PublishedProductSortBy.DisplayOrder => filter.Descending
                ? query.OrderByDescending(product => product.DisplayOrder)
                : query.OrderBy(product => product.DisplayOrder),
            PublishedProductSortBy.Title => filter.Descending
                ? query.OrderByDescending(product => product.Title)
                : query.OrderBy(product => product.Title),
            _ => filter.Descending
                ? query.OrderByDescending(product => product.CreatedAt)
                : query.OrderBy(product => product.CreatedAt)
        };

    // Burada veritabanı projeksiyonunu storefront kartı DTO'suna dönüştürüyorum.
    private static PublishedProductListItemDto ToDto(PublishedProductProjection product) =>
        new(
            PublicIdCodec.EncodeProductId(product.Id),
            product.Title,
            product.Url,
            product.Summary,
            product.BrandName,
            product.Price?.Price,
            product.Price?.CompareAtPrice,
            product.AverageRating,
            product.RatingCount,
            product.MainImage is null
                ? null
                : new ProductImageDto(
                    product.MainImage.Id,
                    PublicIdCodec.EncodeProductId(product.Id),
                    product.MainImage.ImageUrl,
                    product.MainImage.AltText,
                    product.MainImage.DisplayOrder,
                    product.MainImage.IsMain));

    // Burada storefront kartı için gereken ürün alanlarını taşıyorum.
    private sealed record PublishedProductProjection(
        long Id,
        string Title,
        string Url,
        string? Summary,
        string? BrandName,
        decimal AverageRating,
        long RatingCount,
        ProductPriceProjection? Price,
        ProductImageProjection? MainImage);

    // Burada storefront kartının fiyat özetini taşıyorum.
    private sealed record ProductPriceProjection(decimal Price, decimal? CompareAtPrice);

    // Burada storefront kartının ana görsel alanlarını taşıyorum.
    private sealed record ProductImageProjection(
        Guid Id,
        string ImageUrl,
        string? AltText,
        int DisplayOrder,
        bool IsMain);
}
