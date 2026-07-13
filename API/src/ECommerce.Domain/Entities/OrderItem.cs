using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductTitleSnapshot { get; private set; } = null!;
    public string VariantSkuSnapshot { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }

    private OrderItem()
    {
    }

    public OrderItem(
        Guid orderId,
        Guid productId,
        Guid productVariantId,
        string productTitleSnapshot,
        string variantSkuSnapshot,
        decimal unitPrice,
        int quantity)
    {
        if (orderId == Guid.Empty || productId == Guid.Empty || productVariantId == Guid.Empty)
        {
            throw new DomainException("Order, product and variant ids are required.");
        }

        if (string.IsNullOrWhiteSpace(productTitleSnapshot) || string.IsNullOrWhiteSpace(variantSkuSnapshot))
        {
            throw new DomainException("Order item snapshot fields cannot be empty.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainException("Unit price must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        OrderId = orderId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductTitleSnapshot = productTitleSnapshot.Trim();
        VariantSkuSnapshot = variantSkuSnapshot.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        TotalPrice = unitPrice * quantity;
    }
}
