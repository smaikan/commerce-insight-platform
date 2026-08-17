namespace ECommerce.Application.Common.Models;

// Burada public kategori vitrininin sayfalama ve storefront görünürlük seçeneklerini taşıyorum.
public sealed record PublishedProductTypeShowcaseFilter(
    int PageNumber,
    int PageSize,
    bool ShowOutOfStockProducts,
    bool ShowProductsWithoutPrice);
