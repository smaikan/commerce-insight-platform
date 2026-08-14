namespace ECommerce.Application.Products.Dtos;

// Burada navbar aramasının düşük payload'lı tek ürün önerisini tanımlıyorum.
public sealed record PublishedProductSearchSuggestionItemDto(
    string Id,
    string Title,
    string Url,
    string? BrandName,
    decimal? Price,
    decimal? CompareAtPrice,
    string? ImageUrl,
    string? ImageAlt,
    bool IsAvailable);

// Burada öneri listesini toplam sayım yapmadan hasMore bilgisiyle taşıyorum.
public sealed record PublishedProductSearchSuggestionsDto(
    IReadOnlyList<PublishedProductSearchSuggestionItemDto> Items,
    bool HasMore);
