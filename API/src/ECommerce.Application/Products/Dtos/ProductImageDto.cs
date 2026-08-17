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
    // Burada ürün görselini public ürün kimliğiyle birlikte istemci DTO'suna dönüştürüyorum.
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

    // Burada katalog ve sepet cevapları için aynı deterministik ana görsel seçimini uyguluyorum.
    public static ProductImageDto? ToMainImageDto(this IEnumerable<ProductImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        return images
            .OrderByDescending(image => image.IsMain)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => image.ToDto())
            .FirstOrDefault();
    }
}
