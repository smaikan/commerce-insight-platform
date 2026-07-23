using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Cart Cart { get; private set; } = null!;
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public DateTime CreatedAt { get; private set; }

    private CartItem()
    {
    }

    public CartItem(Guid cartId, long productId, Guid productVariantId, int quantity, decimal unitPrice)
    {
        if (cartId == Guid.Empty || productId <= 0 || productVariantId == Guid.Empty)
        {
            throw new DomainException("Cart, product and variant ids are required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainException("Unit price must be greater than zero.");
        }

        CartId = cartId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public void UpdateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
        {
            throw new DomainException("Unit price must be greater than zero.");
        }

        UnitPrice = unitPrice;
    }
}
