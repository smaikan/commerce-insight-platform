using ECommerce.Domain.Entities;

namespace ECommerce.Application.Collections.Dtos;

public sealed record CollectionDto(
    Guid Id,
    string Name,
    string? Description,
    string Url,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder);

public static class CollectionDtoMapping
{
    public static CollectionDto ToDto(this Collection collection)
    {
        return new CollectionDto(
            collection.Id,
            collection.Name,
            collection.Description,
            collection.Url,
            collection.IsActive,
            collection.IsFeatured,
            collection.DisplayOrder);
    }
}
