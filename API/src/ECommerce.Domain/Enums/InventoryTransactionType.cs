namespace ECommerce.Domain.Enums;

public enum InventoryTransactionType
{
    StockIn = 0,
    StockOut = 1,
    OrderCreated = 2,
    OrderCancelled = 3,
    ManualAdjustment = 4,
    Return = 5
}
