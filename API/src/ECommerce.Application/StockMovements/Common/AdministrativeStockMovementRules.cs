using ECommerce.Domain.Enums;

namespace ECommerce.Application.StockMovements.Common;

public static class AdministrativeStockMovementRules
{
    // Burada yalnızca back-office tarafından güvenle oluşturulabilen hareket türlerini ayırıyorum.
    public static bool IsAllowedType(StockMovementType type)
    {
        return type is StockMovementType.Purchase or
            StockMovementType.PurchaseReturn or
            StockMovementType.ManualAdjustment or
            StockMovementType.StockCountAdjustment or
            StockMovementType.Loss or
            StockMovementType.Damage or
            StockMovementType.Expired or
            StockMovementType.TransferIn or
            StockMovementType.TransferOut;
    }

    // Burada hareket türü ile imzalı stok miktarının aynı yönü ifade ettiğini doğruluyorum.
    public static bool HasCompatibleDirection(StockMovementType type, int quantityDelta)
    {
        return type switch
        {
            StockMovementType.Purchase or StockMovementType.TransferIn => quantityDelta > 0,
            StockMovementType.PurchaseReturn or
                StockMovementType.Loss or
                StockMovementType.Damage or
                StockMovementType.Expired or
                StockMovementType.TransferOut => quantityDelta < 0,
            StockMovementType.ManualAdjustment or StockMovementType.StockCountAdjustment =>
                quantityDelta is not 0 and not int.MinValue,
            _ => false
        };
    }
}
