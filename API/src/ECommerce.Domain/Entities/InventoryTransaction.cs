using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class InventoryTransaction : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public InventoryTransactionType Type { get; private set; }
    public int Quantity { get; private set; }
    public int StockAfterTransaction { get; private set; }
    public string? Reason { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InventoryTransaction()
    {
    }

    public InventoryTransaction(
        Guid productVariantId,
        InventoryTransactionType type,
        int quantity,
        int stockAfterTransaction,
        string? reason = null,
        Guid? orderId = null)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new DomainException("Product variant id is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (stockAfterTransaction < 0)
        {
            throw new DomainException("Stock after transaction cannot be negative.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        ProductVariantId = productVariantId;
        Type = type;
        Quantity = quantity;
        StockAfterTransaction = stockAfterTransaction;
        Reason = reason?.Trim();
        OrderId = orderId;
        CreatedAt = DateTime.UtcNow;
    }
}
