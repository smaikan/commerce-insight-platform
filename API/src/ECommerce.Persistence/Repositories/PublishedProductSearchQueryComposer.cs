using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Search;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada tam arama ve önerilerin ortak aday bulma ve relevance semantiğini kuruyorum.
internal static class PublishedProductSearchQueryComposer
{
    // Burada ilk indeksli gramla adayları daraltıp bütün tokenları SQL tarafında AND mantığıyla doğruluyorum.
    public static IQueryable<PublishedProductSearchCandidate> ApplySearch(
        AppDbContext context,
        IQueryable<Product> products,
        string normalizedQuery,
        IReadOnlyList<string> tokens,
        IReadOnlyList<string> candidateGrams)
    {
        var firstGram = candidateGrams[0];

        var candidates =
            from product in products
            join document in context.ProductSearchDocuments.AsNoTracking()
                on product.Id equals document.ProductId
            join gram in context.ProductSearchGrams.AsNoTracking()
                on product.Id equals gram.ProductId
            where gram.Gram == firstGram
            select new PublishedProductSearchCandidate
            {
                Product = product,
                Document = document
            };

        if (context.Database.IsSqlServer())
        {
            return candidates.Where(candidate => AppDbContext.ProductSearchContainsAllTokens(
                candidate.Document.SearchTextNormalized,
                normalizedQuery));
        }

        foreach (var token in tokens)
        {
            var currentToken = token;
            candidates = candidates.Where(candidate =>
                candidate.Document.SearchTextNormalized.Contains(currentToken));
        }

        return candidates;
    }

    // Burada belgelenmiş eşleşme önceliklerini ve kararlı son sıralamayı SQL tarafında uyguluyorum.
    public static IOrderedQueryable<PublishedProductSearchCandidate> OrderByRelevance(
        this IQueryable<PublishedProductSearchCandidate> candidates,
        string normalizedQuery) =>
        candidates
            .OrderByDescending(candidate => candidate.Document.TitleNormalized == normalizedQuery)
            .ThenByDescending(candidate => candidate.Document.TitleNormalized.StartsWith(normalizedQuery))
            .ThenByDescending(candidate => candidate.Document.TitleNormalized.Contains(normalizedQuery))
            .ThenByDescending(candidate => candidate.Document.BrandNormalized.Contains(normalizedQuery))
            .ThenByDescending(candidate => candidate.Document.TypeNormalized.Contains(normalizedQuery))
            .ThenByDescending(candidate => candidate.Document.CollectionNamesNormalized.Contains(normalizedQuery))
            .ThenByDescending(candidate => candidate.Document.TagNamesNormalized.Contains(normalizedQuery))
            .ThenByDescending(candidate => candidate.Product.PopularityScore)
            .ThenBy(candidate => candidate.Product.DisplayOrder)
            .ThenBy(candidate => candidate.Product.Id);
}

// Burada arama dokümanı ile public ürün satırını relevance sıralaması için birlikte taşıyorum.
internal sealed class PublishedProductSearchCandidate
{
    public Product Product { get; init; } = null!;
    public ProductSearchDocument Document { get; init; } = null!;
}
