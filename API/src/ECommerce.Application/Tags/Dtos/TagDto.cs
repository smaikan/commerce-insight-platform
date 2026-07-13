using ECommerce.Domain.Entities;

namespace ECommerce.Application.Tags.Dtos;

public sealed record TagDto(
    Guid Id,
    string Name,
    string Url,
    bool IsActive);

public static class TagDtoMapping
{
    public static TagDto ToDto(this Tag tag)
    {
        return new TagDto(
            tag.Id,
            tag.Name,
            tag.Url,
            tag.IsActive);
    }
}
