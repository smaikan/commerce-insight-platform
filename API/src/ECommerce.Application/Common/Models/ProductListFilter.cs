using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

// Burada admin ürün listesinin sayfalama, filtreleme ve sıralama seçeneklerini taşıyorum.
public sealed record ProductListFilter(
    int PageNumber,
    int PageSize,
    string? Search = null,
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null,
    ProductStatus? Status = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    ProductSortBy SortBy = ProductSortBy.CreatedAt,
    bool Descending = true);

// Burada admin ürün listesinin desteklediği sıralama alanlarını tanımlıyorum.
public enum ProductSortBy
{
    DisplayOrder,
    Title,
    CreatedAt,
    PopularityScore
}
