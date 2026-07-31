using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.StockMovements.Dtos;

// Burada back-office ekranına dönecek değişmez stok hareketi kaydını tanımlıyorum.
public sealed record StockMovementDto(
    Guid Id,
    Guid ProductVariantId,
    StockMovementDirection Direction,
    StockMovementType Type,
    int QuantityDelta,
    int StockBeforeMovement,
    int StockAfterMovement,
    string? Reason,
    Guid? OrderId,
    Guid? ReturnRequestId,
    DateTime CreatedAt);

// Burada kayıtlı stok bakiyesi ile hareket toplamının mutabakat sonucunu tanımlıyorum.
public sealed record StockBalanceDto(
    Guid ProductVariantId,
    int PersistedStock,
    long MovementBalance,
    bool IsConsistent);

public static class StockMovementDtoMapping
{
    // Burada stok hareketi entity'sini güvenli back-office cevap modeline dönüştürüyorum.
    public static StockMovementDto ToDto(this StockMovement movement)
    {
        return new StockMovementDto(
            movement.Id,
            movement.ProductVariantId,
            movement.Direction,
            movement.Type,
            movement.QuantityDelta,
            movement.StockBeforeMovement,
            movement.StockAfterMovement,
            movement.Reason,
            movement.OrderId,
            movement.ReturnRequestId,
            movement.CreatedAt);
    }
}
