using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada yayımlanmış katalog facetlerini sayfalama sonucundan bağımsız olarak veritabanında hesaplıyorum.
public sealed class PublishedProductFacetReader : IPublishedProductFacetReader
{
    private readonly AppDbContext _context;

    // Burada facet sorgularının çalışacağı salt okunur DbContext'i hazırlıyorum.
    public PublishedProductFacetReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada seçilen facet boyutunu kendi filtresinden bağımsız, diğer filtrelerle birlikte hesaplıyorum.
    public async Task<IReadOnlyList<PublishedProductFacetItemDto>> GetFacetsAsync(
        PublishedProductFacetDimension dimension,
        PublishedProductFacetFilter filter,
        CancellationToken cancellationToken = default)
    {
        var products = _context.Products
            .AsNoTracking()
            .WherePublished()
            .ApplyStorefrontVisibility(
                filter.ShowOutOfStockProducts,
                filter.ShowProductsWithoutPrice);
        products = ApplyFilters(products, filter, dimension);

        return dimension switch
        {
            PublishedProductFacetDimension.Brand => await GetBrandFacetsAsync(products, cancellationToken),
            PublishedProductFacetDimension.Collection => await GetCollectionFacetsAsync(products, cancellationToken),
            PublishedProductFacetDimension.ProductType => await GetProductTypeFacetsAsync(products, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unsupported facet dimension.")
        };
    }

    // Burada marka facetlerini yalnız aktif markalardan ve en az bir eşleşen üründen üretiyorum.
    private static async Task<IReadOnlyList<PublishedProductFacetItemDto>> GetBrandFacetsAsync(
        IQueryable<Product> products,
        CancellationToken cancellationToken)
    {
        var facets = await products
            .Where(product => product.BrandId.HasValue && product.Brand != null && product.Brand.IsActive)
            .GroupBy(product => new { Id = product.BrandId!.Value, product.Brand!.Name })
            .Select(group => new { group.Key.Id, group.Key.Name, ProductCount = group.Count() })
            .OrderBy(facet => facet.Name)
            .ThenBy(facet => facet.Id)
            .ToListAsync(cancellationToken);
        return facets
            .Select(facet => new PublishedProductFacetItemDto(facet.Id, facet.Name, facet.ProductCount))
            .ToList();
    }

    // Burada koleksiyon facetlerini benzersiz ürün-koleksiyon ilişkileri üzerinden adetli getiriyorum.
    private static async Task<IReadOnlyList<PublishedProductFacetItemDto>> GetCollectionFacetsAsync(
        IQueryable<Product> products,
        CancellationToken cancellationToken)
    {
        var facets = await products
            .SelectMany(product => product.ProductCollections)
            .Where(relation => relation.Collection.IsActive)
            .GroupBy(relation => new { relation.CollectionId, relation.Collection.Name })
            .Select(group => new { Id = group.Key.CollectionId, group.Key.Name, ProductCount = group.Count() })
            .OrderBy(facet => facet.Name)
            .ThenBy(facet => facet.Id)
            .ToListAsync(cancellationToken);
        return facets
            .Select(facet => new PublishedProductFacetItemDto(facet.Id, facet.Name, facet.ProductCount))
            .ToList();
    }

    // Burada ürün türü facetlerini yalnız aktif türlerden ve en az bir eşleşen üründen üretiyorum.
    private static async Task<IReadOnlyList<PublishedProductFacetItemDto>> GetProductTypeFacetsAsync(
        IQueryable<Product> products,
        CancellationToken cancellationToken)
    {
        var facets = await products
            .Where(product => product.TypeId.HasValue && product.Type != null && product.Type.IsActive)
            .GroupBy(product => new { Id = product.TypeId!.Value, product.Type!.Name })
            .Select(group => new { group.Key.Id, group.Key.Name, ProductCount = group.Count() })
            .OrderBy(facet => facet.Name)
            .ThenBy(facet => facet.Id)
            .ToListAsync(cancellationToken);
        return facets
            .Select(facet => new PublishedProductFacetItemDto(facet.Id, facet.Name, facet.ProductCount))
            .ToList();
    }

    // Burada seçili facetin kendi boyutunu dışarıda bırakıp diğer aktif sınıflandırma filtrelerini uyguluyorum.
    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> products,
        PublishedProductFacetFilter filter,
        PublishedProductFacetDimension dimension)
    {
        if (dimension != PublishedProductFacetDimension.ProductType && filter.TypeId.HasValue)
        {
            products = products.Where(product =>
                product.TypeId == filter.TypeId.Value &&
                product.Type != null &&
                product.Type.IsActive);
        }

        if (dimension != PublishedProductFacetDimension.Brand && filter.BrandId.HasValue)
        {
            products = products.Where(product =>
                product.BrandId == filter.BrandId.Value &&
                product.Brand != null &&
                product.Brand.IsActive);
        }

        if (dimension != PublishedProductFacetDimension.Collection && filter.CollectionId.HasValue)
        {
            products = products.Where(product => product.ProductCollections.Any(
                relation =>
                    relation.CollectionId == filter.CollectionId.Value &&
                    relation.Collection.IsActive));
        }

        if (filter.TagId.HasValue)
        {
            products = products.Where(product => product.ProductTags.Any(
                relation => relation.TagId == filter.TagId.Value && relation.Tag.IsActive));
        }

        return products;
    }
}
