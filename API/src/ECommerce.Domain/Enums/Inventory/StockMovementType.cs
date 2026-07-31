namespace ECommerce.Domain.Enums;

public enum StockMovementType
{
    OpeningBalance = 1,

    Purchase = 10,
    PurchaseReturn = 11,

    Sale = 20,
    SaleReturn = 21,
    AccountingSale = 22,
    AccountingSaleCancellation = 23,

    ManualAdjustment = 30,
    StockCountAdjustment = 31,

    Loss = 40,
    Damage = 41,
    Expired = 42,

    TransferIn = 50,
    TransferOut = 51,

    Cancellation = 60
}
