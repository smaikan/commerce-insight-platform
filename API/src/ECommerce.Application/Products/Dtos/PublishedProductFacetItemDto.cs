namespace ECommerce.Application.Products.Dtos;

// Burada tek bir yayımlanmış ürün facet seçeneğini toplam ürün adediyle taşıyorum.
public sealed record PublishedProductFacetItemDto(
    Guid Id,
    string Name,
    int ProductCount);
