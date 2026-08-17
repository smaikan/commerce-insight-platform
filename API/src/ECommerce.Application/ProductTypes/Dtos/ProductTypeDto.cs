using ECommerce.Domain.Entities;

namespace ECommerce.Application.ProductTypes.Dtos;

public sealed record ProductTypeDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    string? ImageUrl);

public static class ProductTypeDtoMapping
{
    // Burada ürün türü entity'sini yönetim ve standart liste DTO'suna dönüştürüyorum.
    public static ProductTypeDto ToDto(this ProductType productType)
    {
        return new ProductTypeDto(
            productType.Id,
            productType.Name,
            productType.Description,
            productType.IsActive,
            productType.ImageUrl);
    }
}
