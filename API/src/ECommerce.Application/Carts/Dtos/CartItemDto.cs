using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.Carts.Dtos;

// Burada bir sepet satırının kayıtlı ve güncel katalog değerlerini birlikte taşıyan cevap modelini tanımlıyorum.
public sealed record CartItemDto(
    Guid Id,
    string ProductId,
    Guid ProductVariantId,
    string? ProductTitle,
    [property: MaxLength(OrderItem.MaximumVariantNameLength)] string? VariantName,
    [property: MaxLength(OrderItem.MaximumVariantValueLength)] string? VariantValue,
    string? Sku,
    int Quantity,
    decimal UnitPrice,
    decimal CurrentUnitPrice,
    decimal TotalPrice,
    int AvailableStock,
    bool IsAvailable,
    bool PriceChanged,
    DateTime CreatedAt);

public static class CartItemDtoMapping
{
    // Burada sepet satırını public ürün kimliği ve güncel satış bilgileriyle DTO'ya dönüştürüyorum.
    public static CartItemDto ToDto(this CartItem item)
    {
        var product = item.Product;
        var variant = item.ProductVariant;
        var exposesVariantSelection = product?.HasVariants == true;
        var currentUnitPrice = variant?.Price ?? item.UnitPrice;
        var isAvailable =
            product is not null &&
            variant is not null &&
            variant.ProductId == item.ProductId &&
            product.IsActive &&
            product.Status == ProductStatus.Active &&
            variant.IsActive &&
            variant.Stock >= item.Quantity;

        return new CartItemDto(
            item.Id,
            PublicIdCodec.EncodeProductId(item.ProductId),
            item.ProductVariantId,
            product?.Title,
            exposesVariantSelection ? variant?.Name : null,
            exposesVariantSelection ? variant?.Value : null,
            variant?.Sku,
            item.Quantity,
            item.UnitPrice,
            currentUnitPrice,
            item.TotalPrice,
            variant?.Stock ?? 0,
            isAvailable,
            currentUnitPrice != item.UnitPrice,
            item.CreatedAt);
    }
}
