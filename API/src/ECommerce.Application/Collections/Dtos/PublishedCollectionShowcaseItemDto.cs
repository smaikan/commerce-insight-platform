using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.Collections.Dtos;

// Burada storefront koleksiyon kartının toplu public sözleşmesini tanımlıyorum.
public sealed record PublishedCollectionShowcaseItemDto(
    Guid Id,
    [property: MaxLength(150)] string Name,
    [property: MaxLength(200)] string Url,
    int ProductCount,
    bool IsFeatured,
    int DisplayOrder,
    [property: MaxLength(500)] string? ImageUrl);
