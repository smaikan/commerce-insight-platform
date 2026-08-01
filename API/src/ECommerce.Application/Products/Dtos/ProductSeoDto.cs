using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductSeoDto(
    ProductDto Product,
    IReadOnlyList<ProductImageDto> Images,
    DateTime LastModifiedAt);

public sealed record ProductSeoIndexItemDto(string Url, DateTime LastModifiedAt);

public static class ProductSeoDtoMapping
{
    public static ProductSeoDto ToSeoDto(this Product product)
    {
        return new ProductSeoDto(
            product.ToDto(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.ToDto())
                .ToList(),
            product.UpdatedAt ?? product.CreatedAt);
    }
}
