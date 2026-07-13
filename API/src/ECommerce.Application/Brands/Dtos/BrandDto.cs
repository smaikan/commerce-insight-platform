using ECommerce.Domain.Entities;

namespace ECommerce.Application.Brands.Dtos;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string? Description,
    string Url,
    bool IsActive);

public static class BrandDtoMapping
{
    public static BrandDto ToDto(this Brand brand)
    {
        return new BrandDto(
            brand.Id,
            brand.Name,
            brand.Description,
            brand.Url,
            brand.IsActive);
    }
}
