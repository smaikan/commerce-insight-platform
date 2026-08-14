namespace ECommerce.Application.Products.Dtos;

// Burada storefront kartlarının ihtiyaç duyduğu sınırlı ürün bilgisini tanımlıyorum.
public sealed record PublishedProductListItemDto(
    string Id,
    string Title,
    string Url,
    string? Summary,
    string? BrandName,
    decimal? Price,
    decimal? CompareAtPrice,
    decimal AverageRating,
    long RatingCount,
    ProductImageDto? MainImage,
    bool IsAvailable,
    int? LowestAvailableStock,
    bool IsLowStock);
