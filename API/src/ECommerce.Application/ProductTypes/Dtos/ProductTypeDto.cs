using ECommerce.Domain.Entities;

namespace ECommerce.Application.ProductTypes.Dtos;

public sealed record ProductTypeDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public static class ProductTypeDtoMapping
{
    public static ProductTypeDto ToDto(this ProductType productType)
    {
        return new ProductTypeDto(
            productType.Id,
            productType.Name,
            productType.Description,
            productType.IsActive);
    }
}
