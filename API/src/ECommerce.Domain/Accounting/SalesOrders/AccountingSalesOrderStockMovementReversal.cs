using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Accounting.SalesOrders;

public sealed class AccountingSalesOrderStockMovementReversal : BaseEntity
{
    public Guid AccountingSalesOrderId { get; private set; }
    public AccountingSalesOrder AccountingSalesOrder { get; private set; } = null!;
    public Guid OriginalStockMovementId { get; private set; }
    public StockMovement OriginalStockMovement { get; private set; } = null!;
    public Guid ReversalStockMovementId { get; private set; }
    public StockMovement ReversalStockMovement { get; private set; } = null!;
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AccountingSalesOrderStockMovementReversal() { }

    public AccountingSalesOrderStockMovementReversal(
        AccountingSalesOrder order,
        StockMovement original,
        StockMovement reversal)
    {
        if (order is null || original is null || reversal is null ||
            original.Type != StockMovementType.AccountingSale ||
            reversal.Type != StockMovementType.AccountingSaleCancellation ||
            original.ProductVariantId != reversal.ProductVariantId ||
            original.QuantityDelta != -reversal.QuantityDelta)
        {
            throw new DomainException("Matching original and reversal accounting stock movements are required.");
        }

        AccountingSalesOrderId = order.Id;
        AccountingSalesOrder = order;
        OriginalStockMovementId = original.Id;
        OriginalStockMovement = original;
        ReversalStockMovementId = reversal.Id;
        ReversalStockMovement = reversal;
        Quantity = reversal.QuantityDelta;
        CreatedAt = DateTime.UtcNow;
    }
}
