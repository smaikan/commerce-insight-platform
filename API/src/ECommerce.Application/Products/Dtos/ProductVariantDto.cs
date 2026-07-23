using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductVariantDto(
    Guid Id,
    string ProductId,
    string Name,
    string Sku,
    string? Barcode,
    string? Material,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    long AddToCartCount,
    long PurchaseCount,
    bool IsActive);

public static class ProductVariantDtoMapping
{
    // Burada varyant entity'sini ürün public kimliğiyle DTO'ya dönüştürüyorum.
    public static ProductVariantDto ToDto(this ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            PublicIdCodec.EncodeProductId(variant.Product?.Id ?? variant.ProductId),
            variant.Name,
            variant.Sku,
            variant.Barcode,
            variant.Material,
            variant.Price,
            variant.CompareAtPrice,
            variant.Stock,
            variant.AddToCartCount,
            variant.PurchaseCount,
            variant.IsActive);
    }
}
