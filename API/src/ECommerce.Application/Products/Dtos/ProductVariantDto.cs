using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Barcode,
    string? Color,
    string? Size,
    string? Material,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    int AddToCartCount,
    int PurchaseCount,
    bool IsActive);

public static class ProductVariantDtoMapping
{
    public static ProductVariantDto ToDto(this ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Barcode,
            variant.Color,
            variant.Size,
            variant.Material,
            variant.Price,
            variant.CompareAtPrice,
            variant.Stock,
            variant.AddToCartCount,
            variant.PurchaseCount,
            variant.IsActive);
    }
}
