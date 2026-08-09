namespace ECommerce.Application.Common.Models;

// Burada storefront ürün listesinin sayfalama ve sıralama seçeneklerini taşıyorum.
public sealed record PublishedProductListFilter(
    int PageNumber,
    int PageSize,
    PublishedProductSortBy SortBy = PublishedProductSortBy.Newest,
    bool Descending = true);

public enum PublishedProductSortBy
{
    Newest,
    Popularity,
    DisplayOrder,
    Title
}
