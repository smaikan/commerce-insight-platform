using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

public sealed record ProductListFilter(
    int PageNumber,
    int PageSize,
    string? Search = null,
    Guid? TypeId = null,
    Guid? BrandId = null,
    ProductStatus? Status = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    ProductSortBy SortBy = ProductSortBy.CreatedAt,
    bool Descending = true);

public enum ProductSortBy
{
    DisplayOrder,
    Title,
    CreatedAt,
    PopularityScore
}
