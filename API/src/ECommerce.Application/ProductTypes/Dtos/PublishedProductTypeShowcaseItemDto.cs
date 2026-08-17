using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.ProductTypes.Dtos;

// Burada storefront kategori kartının toplu public sözleşmesini tanımlıyorum.
public sealed record PublishedProductTypeShowcaseItemDto(
    Guid Id,
    [property: MaxLength(150)] string Name,
    int ProductCount,
    [property: MaxLength(500)] string? ImageUrl);
