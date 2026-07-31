using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Accounting.SalesOrders;

public sealed class AccountingSalesOrderStockMovement : BaseEntity
{
    public Guid AccountingSalesOrderItemId { get; private set; }
    public AccountingSalesOrderItem AccountingSalesOrderItem { get; private set; } = null!;
    public Guid StockMovementId { get; private set; }
    public StockMovement StockMovement { get; private set; } = null!;
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un satış satırı stok hareketi bağını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private AccountingSalesOrderStockMovement()
    {
    }

    // Burada mevcut negatif stok hareketini değiştirmeden ilgili Accounting satış satırına bağlıyorum.
    internal AccountingSalesOrderStockMovement(
        AccountingSalesOrderItem item,
        StockMovement stockMovement)
    {
        if (item is null ||
            stockMovement is null ||
            item.Id == Guid.Empty ||
            stockMovement.Id == Guid.Empty ||
            item.ProductVariantId != stockMovement.ProductVariantId ||
            stockMovement.Type != StockMovementType.AccountingSale ||
            stockMovement.QuantityDelta >= 0)
        {
            throw new DomainException(
                "A matching sales item and AccountingSale stock-out movement are required.");
        }

        AccountingSalesOrderItemId = item.Id;
        AccountingSalesOrderItem = item;
        StockMovementId = stockMovement.Id;
        StockMovement = stockMovement;
        Quantity = checked(-stockMovement.QuantityDelta);
        CreatedAt = DateTime.UtcNow;
    }
}
