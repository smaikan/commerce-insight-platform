using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada public kategori vitrinini adet ve etkili görselle veritabanından toplu projekte ediyorum.
public sealed class PublishedProductTypeShowcaseReader : IPublishedProductTypeShowcaseReader
{
    private readonly AppDbContext _context;

    // Burada kategori vitrin sorgularının çalışacağı salt okunur DbContext'i hazırlıyorum.
    public PublishedProductTypeShowcaseReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada aktif ve yayımlanmış ürünü bulunan kategorileri popüler ürün fallback'iyle getiriyorum.
    public async Task<PagedResult<PublishedProductTypeShowcaseItemDto>> GetListAsync(
        PublishedProductTypeShowcaseFilter filter,
        CancellationToken cancellationToken = default)
    {
        var publishedProducts = _context.Products
            .AsNoTracking()
            .WherePublished()
            .ApplyStorefrontVisibility(
                filter.ShowOutOfStockProducts,
                filter.ShowProductsWithoutPrice);
        var productTypes = _context.ProductTypes
            .AsNoTracking()
            .Where(productType =>
                productType.IsActive &&
                publishedProducts.Any(product => product.TypeId == productType.Id));

        var totalCount = await productTypes.CountAsync(cancellationToken);
        var items = await productTypes
            .OrderBy(productType => productType.Name)
            .ThenBy(productType => productType.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(productType => new PublishedProductTypeShowcaseItemDto(
                productType.Id,
                productType.Name,
                publishedProducts.Count(product => product.TypeId == productType.Id),
                productType.ImageUrl ?? publishedProducts
                    .Where(product => product.TypeId == productType.Id)
                    .OrderByDescending(product => product.PopularityScore)
                    .ThenBy(product => product.Id)
                    .Select(product => product.Images
                        .OrderByDescending(image => image.IsMain)
                        .ThenBy(image => image.DisplayOrder)
                        .ThenBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault())
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublishedProductTypeShowcaseItemDto>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }
}
