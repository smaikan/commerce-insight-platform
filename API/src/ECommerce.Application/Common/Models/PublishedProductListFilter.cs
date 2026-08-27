namespace ECommerce.Application.Common.Models;

// Burada storefront ürün listesinin sayfalama ve sıralama seçeneklerini taşıyorum.
public sealed record PublishedProductListFilter(
    int PageNumber,
    int PageSize,
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null,
    PublishedProductSortBy? SortBy = PublishedProductSortBy.Newest,
    bool? Descending = true,
    bool ShowOutOfStockProducts = true,
    bool ShowProductsWithoutPrice = true,
    bool ShowStockWarning = false,
    int LowStockThreshold = 5,
    string? SearchNormalized = null,
    IReadOnlyList<string>? SearchTokens = null,
    IReadOnlyList<string>? CandidateGrams = null,
    bool ResolveStoreSettings = false);

// Burada storefront ürün listesinin desteklediği sıralama alanlarını tanımlıyorum.
public enum PublishedProductSortBy
{
    Newest = 0,
    Popularity = 1,
    DisplayOrder = 2,
    Title = 3,
    BestSelling = 4
}
