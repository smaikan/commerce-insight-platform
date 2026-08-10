using ECommerce.Domain.Entities;

namespace ECommerce.Application.Brands.Dtos;

// Burada markanın istemciye açılan alanlarını tanımlıyorum.
public sealed record BrandDto(
    Guid Id,
    string Name,
    string? Description,
    string Url,
    bool IsActive,
    string? ImageUrl);

public static class BrandDtoMapping
{
    // Burada marka entity'sini API sözleşmesine dönüştürüyorum.
    public static BrandDto ToDto(this Brand brand)
    {
        return new BrandDto(
            brand.Id,
            brand.Name,
            brand.Description,
            brand.Url,
            brand.IsActive,
            brand.ImageUrl);
    }
}
