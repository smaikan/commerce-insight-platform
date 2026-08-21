using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada public koleksiyon vitrinini adet ve etkili görselle veritabanından toplu projekte ediyorum.
public sealed class PublishedCollectionShowcaseReader : IPublishedCollectionShowcaseReader
{
    private readonly AppDbContext _context;

    // Burada koleksiyon vitrin sorgularının çalışacağı salt okunur DbContext'i hazırlıyorum.
    public PublishedCollectionShowcaseReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada aktif ve yayımlanmış ürünü bulunan koleksiyonları kararlı sıralama ve sabit sorgu sayısıyla getiriyorum.
    public async Task<PagedResult<PublishedCollectionShowcaseItemDto>> GetListAsync(
        PublishedCollectionShowcaseFilter filter,
        CancellationToken cancellationToken = default)
    {
        var publishedProducts = _context.Products
            .AsNoTracking()
            .WherePublished()
            .ApplyStorefrontVisibility(
                filter.ShowOutOfStockProducts,
                filter.ShowProductsWithoutPrice);
        var publishedRelations =
            from relation in _context.ProductCollections.AsNoTracking()
            join product in publishedProducts on relation.ProductId equals product.Id
            select new { relation.CollectionId, Product = product };
        var collections = _context.Collections
            .AsNoTracking()
            .Where(collection => collection.IsActive);

        var totalCount = await collections.CountAsync(cancellationToken);
        var items = await collections
            .OrderBy(collection => collection.DisplayOrder)
            .ThenBy(collection => collection.Name)
            .ThenBy(collection => collection.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(collection => new PublishedCollectionShowcaseItemDto(
                collection.Id,
                collection.Name,
                collection.Url,
                publishedRelations.Count(relation => relation.CollectionId == collection.Id),
                collection.IsFeatured,
                collection.DisplayOrder,
                collection.ImageUrl ?? publishedRelations
                    .Where(relation => relation.CollectionId == collection.Id)
                    .OrderByDescending(relation => relation.Product.PopularityScore)
                    .ThenBy(relation => relation.Product.Id)
                    .Select(relation => relation.Product.Images
                        .OrderByDescending(image => image.IsMain)
                        .ThenBy(image => image.DisplayOrder)
                        .ThenBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault())
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublishedCollectionShowcaseItemDto>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }
}
