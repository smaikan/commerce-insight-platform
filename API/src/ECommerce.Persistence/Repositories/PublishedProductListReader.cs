using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
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
        var settings = _context.StoreSettings
            .AsNoTracking()
            .Where(item => item.Id == StoreSettings.SingletonId);
        var publishedProducts = _context.Products
            .AsNoTracking()
            .WherePublished();
        var query = filter.ResolveStoreSettings
            ? publishedProducts.ApplyStorefrontVisibility(settings)
            : publishedProducts.ApplyStorefrontVisibility(
                filter.ShowOutOfStockProducts,
                filter.ShowProductsWithoutPrice);
        query = ApplyFilter(query, filter);

        int totalCount;
        IQueryable<Product> orderedProducts;
        if (filter.SearchTokens is { Count: > 0 } &&
            filter.CandidateGrams is { Count: > 0 } &&
            filter.SearchNormalized is not null)
        {
            var candidates = PublishedProductSearchQueryComposer.ApplySearch(
                _context,
                query,
                filter.SearchNormalized,
                filter.SearchTokens,
                filter.CandidateGrams);
            totalCount = await candidates.CountAsync(cancellationToken);
            orderedProducts = filter.SortBy.HasValue
                ? CreateOrderedQuery(
                        candidates.Select(candidate => candidate.Product),
                        filter,
                        settings)
                    .ThenBy(product => product.Id)
                : candidates.OrderByRelevance(filter.SearchNormalized)
                    .Select(candidate => candidate.Product);
        }
        else
        {
            totalCount = await query.CountAsync(cancellationToken);
            orderedProducts = CreateOrderedQuery(query, filter, settings).ThenBy(product => product.Id);
        }

        var pagedProducts = orderedProducts
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);
        var projectedProducts = filter.ResolveStoreSettings
            ? pagedProducts.SelectPublishedProductCards(settings)
            : pagedProducts.SelectPublishedProductCards(filter.ShowStockWarning, filter.LowStockThreshold);
        var items = await projectedProducts
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
        PublishedProductListFilter filter,
        IQueryable<StoreSettings> settings)
    {
        if (filter.ResolveStoreSettings && !filter.SortBy.HasValue)
        {
            return CreateStoreDefaultOrderedQuery(query, settings);
        }

        if (filter.ResolveStoreSettings && !filter.Descending.HasValue)
        {
            return CreateStoreDirectionOrderedQuery(
                query,
                filter.SortBy ?? PublishedProductSortBy.Newest,
                settings);
        }

        var descending = filter.Descending != false;
        return filter.SortBy switch
        {
            PublishedProductSortBy.BestSelling => descending
                ? query.OrderByDescending(product => product.NetSalesQuantity)
                : query.OrderBy(product => product.NetSalesQuantity),
            PublishedProductSortBy.Popularity => descending
                ? query.OrderByDescending(product => product.PopularityScore)
                : query.OrderBy(product => product.PopularityScore),
            PublishedProductSortBy.DisplayOrder => descending
                ? query.OrderByDescending(product => product.DisplayOrder)
                : query.OrderBy(product => product.DisplayOrder),
            PublishedProductSortBy.Title => descending
                ? query.OrderByDescending(product => product.Title)
                : query.OrderBy(product => product.Title),
            _ => descending
                ? query.OrderByDescending(product => product.CreatedAt)
                : query.OrderBy(product => product.CreatedAt)
        };
    }

    // Burada StoreSettings içindeki varsayılan alan ve yönü tek SQL'in koşullu sıralama anahtarlarına çeviriyorum.
    private static IOrderedQueryable<Product> CreateStoreDefaultOrderedQuery(
        IQueryable<Product> query,
        IQueryable<StoreSettings> settings) =>
        query
            .OrderByDescending(product =>
                (!settings.Any() || settings.Any(item =>
                    item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Newest &&
                    item.DefaultProductSortDescending))
                    ? product.CreatedAt : DateTime.MinValue)
            .ThenBy(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Newest &&
                !item.DefaultProductSortDescending) ? product.CreatedAt : DateTime.MaxValue)
            .ThenByDescending(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Popularity &&
                item.DefaultProductSortDescending) ? product.PopularityScore : long.MinValue)
            .ThenBy(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Popularity &&
                !item.DefaultProductSortDescending) ? product.PopularityScore : long.MaxValue)
            .ThenByDescending(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.DisplayOrder &&
                item.DefaultProductSortDescending) ? product.DisplayOrder : int.MinValue)
            .ThenBy(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.DisplayOrder &&
                !item.DefaultProductSortDescending) ? product.DisplayOrder : int.MaxValue)
            .ThenByDescending(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Title &&
                item.DefaultProductSortDescending) ? product.Title : string.Empty)
            .ThenBy(product => settings.Any(item =>
                item.DefaultProductSort == ECommerce.Domain.Enums.StorefrontProductSort.Title &&
                !item.DefaultProductSortDescending) ? product.Title : string.Empty);

    // Burada explicit alanın yönü gönderilmediyse yalnız StoreSettings yönünü SQL tarafında uyguluyorum.
    private static IOrderedQueryable<Product> CreateStoreDirectionOrderedQuery(
        IQueryable<Product> query,
        PublishedProductSortBy sortBy,
        IQueryable<StoreSettings> settings) =>
        sortBy switch
        {
            PublishedProductSortBy.BestSelling => query
                .OrderByDescending(product =>
                    (!settings.Any() || settings.Any(item => item.DefaultProductSortDescending))
                        ? product.NetSalesQuantity : long.MinValue)
                .ThenBy(product => settings.Any(item => !item.DefaultProductSortDescending)
                    ? product.NetSalesQuantity : long.MaxValue),
            PublishedProductSortBy.Popularity => query
                .OrderByDescending(product =>
                    (!settings.Any() || settings.Any(item => item.DefaultProductSortDescending))
                        ? product.PopularityScore : long.MinValue)
                .ThenBy(product => settings.Any(item => !item.DefaultProductSortDescending)
                    ? product.PopularityScore : long.MaxValue),
            PublishedProductSortBy.DisplayOrder => query
                .OrderByDescending(product =>
                    (!settings.Any() || settings.Any(item => item.DefaultProductSortDescending))
                        ? product.DisplayOrder : int.MinValue)
                .ThenBy(product => settings.Any(item => !item.DefaultProductSortDescending)
                    ? product.DisplayOrder : int.MaxValue),
            PublishedProductSortBy.Title => query
                .OrderByDescending(product =>
                    (!settings.Any() || settings.Any(item => item.DefaultProductSortDescending))
                        ? product.Title : string.Empty)
                .ThenBy(product => settings.Any(item => !item.DefaultProductSortDescending)
                    ? product.Title : string.Empty),
            _ => query
                .OrderByDescending(product =>
                    (!settings.Any() || settings.Any(item => item.DefaultProductSortDescending))
                        ? product.CreatedAt : DateTime.MinValue)
                .ThenBy(product => settings.Any(item => !item.DefaultProductSortDescending)
                    ? product.CreatedAt : DateTime.MaxValue)
        };

    // Burada veritabanı projeksiyonunu storefront kartı DTO'suna dönüştürüyorum.
    private static PublishedProductListItemDto ToDto(PublishedProductCardProjection product) =>
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
                    product.MainImage.IsMain),
            product.IsAvailable,
            product.LowestAvailableStock,
            product.IsLowStock);
}
