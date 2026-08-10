using ECommerce.Domain.Entities;

namespace ECommerce.Application.Collections.Dtos;

// Burada koleksiyonun istemciye açılan alanlarını tanımlıyorum.
public sealed record CollectionDto(
    Guid Id,
    string Name,
    string? Description,
    string Url,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    string? ImageUrl);

public static class CollectionDtoMapping
{
    // Burada koleksiyon entity'sini API sözleşmesine dönüştürüyorum.
    public static CollectionDto ToDto(this Collection collection)
    {
        return new CollectionDto(
            collection.Id,
            collection.Name,
            collection.Description,
            collection.Url,
            collection.IsActive,
            collection.IsFeatured,
            collection.DisplayOrder,
            collection.ImageUrl);
    }
}
