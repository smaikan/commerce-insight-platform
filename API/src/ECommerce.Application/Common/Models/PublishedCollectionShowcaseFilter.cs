namespace ECommerce.Application.Common.Models;

// Burada public koleksiyon vitrininin sayfalama ve storefront görünürlük seçeneklerini taşıyorum.
public sealed record PublishedCollectionShowcaseFilter(
    int PageNumber,
    int PageSize,
    bool ShowOutOfStockProducts,
    bool ShowProductsWithoutPrice);
