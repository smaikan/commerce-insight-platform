using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductVariantDto(
    Guid Id,
    string ProductId,
    string Name,
    string Value,
    Guid? VariantOptionNameId,
    Guid? VariantOptionValueId,
    string Sku,
    string? Barcode,
    string? Material,
    decimal Price,
    decimal NetPrice,
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
            variant.Value,
            variant.VariantOptionNameId,
            variant.VariantOptionValueId,
            variant.Sku,
            variant.Barcode,
            variant.Material,
            variant.Price,
            variant.NetPrice,
            variant.CompareAtPrice,
            variant.Stock,
            variant.AddToCartCount,
            variant.PurchaseCount,
            variant.IsActive);
    }
}
