using ECommerce.Domain.Enums;

namespace ECommerce.Application.StockMovements.Dtos;

// Burada stok defteri ekranının ihtiyaç duyduğu ürün ve varyant bağlamlı satırı tanımlıyorum.
public sealed record StockMovementListItemDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantName,
    string VariantValue,
    string Sku,
    StockMovementDirection Direction,
    StockMovementType Type,
    int QuantityDelta,
    int StockBeforeMovement,
    int StockAfterMovement,
    string? Reason,
    Guid? OrderId,
    Guid? ReturnRequestId,
    DateTime CreatedAt);
