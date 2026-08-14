using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada navbar önerilerini tek, izlenmeyen ve düşük kolonlu SQL sorgusuyla okuyorum.
public sealed class PublishedProductSearchReader : IPublishedProductSearchReader
{
    private readonly AppDbContext _context;

    // Burada public öneri sorgusunun DbContext bağımlılığını hazırlıyorum.
    public PublishedProductSearchReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada Limit+1 kaydı tek komutta okuyup COUNT kullanmadan hasMore üretiyorum.
    public async Task<PublishedProductSearchSuggestionsDto> GetSuggestionsAsync(
        PublishedProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var settings = _context.StoreSettings.AsNoTracking()
            .Where(item => item.Id == StoreSettings.SingletonId);
        var products = _context.Products
            .AsNoTracking()
            .WherePublished();
        var searchCandidates = PublishedProductSearchQueryComposer.ApplySearch(
            _context,
            products,
            filter.NormalizedQuery,
            filter.Tokens,
            filter.CandidateGrams);
        var candidates =
            from candidate in searchCandidates
            from setting in settings
            where (setting.ShowOutOfStockProducts || candidate.Product.Variants.Any(variant =>
                    variant.IsActive && variant.Stock > 0)) &&
                (setting.ShowProductsWithoutPrice || candidate.Product.Variants.Any(variant =>
                    variant.IsActive))
            select candidate;
        var rows = await candidates
            .OrderByRelevance(filter.NormalizedQuery)
            .Select(candidate => candidate.Product)
            .Take(filter.Limit + 1)
            .SelectPublishedProductSuggestionCards()
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > filter.Limit;
        var items = rows.Take(filter.Limit)
            .Select(ToDto)
            .ToArray();
        return new PublishedProductSearchSuggestionsDto(items, hasMore);
    }

    // Burada SQL projeksiyonundaki dahili ürün kimliğini public kimliğe çeviriyorum.
    private static PublishedProductSearchSuggestionItemDto ToDto(PublishedProductSuggestionCardProjection product) =>
        new(
            PublicIdCodec.EncodeProductId(product.Id),
            product.Title,
            product.Url,
            product.BrandName,
            product.Price,
            product.CompareAtPrice,
            product.ImageUrl,
            product.ImageAlt,
            product.IsAvailable);
}
