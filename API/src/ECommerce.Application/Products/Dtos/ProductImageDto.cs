using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductImageDto(
    Guid Id,
    string ProductId,
    string ImageUrl,
    string? AltText,
    int DisplayOrder,
    bool IsMain);

public static class ProductImageDtoMapping
{
    public static ProductImageDto ToDto(this ProductImage image)
    {
        return new ProductImageDto(
            image.Id,
            PublicIdCodec.EncodeProductId(image.Product?.Id ?? image.ProductId),
            image.ImageUrl,
            image.AltText,
            image.DisplayOrder,
            image.IsMain);
    }
}
