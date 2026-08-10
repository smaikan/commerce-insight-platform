namespace ECommerce.Application.Common.Models;

// Burada storefront ürün listesinin sayfalama ve sıralama seçeneklerini taşıyorum.
public sealed record PublishedProductListFilter(
    int PageNumber,
    int PageSize,
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null,
    PublishedProductSortBy SortBy = PublishedProductSortBy.Newest,
    bool Descending = true);

// Burada storefront ürün listesinin desteklediği sıralama alanlarını tanımlıyorum.
public enum PublishedProductSortBy
{
    Newest,
    Popularity,
    DisplayOrder,
    Title
}
