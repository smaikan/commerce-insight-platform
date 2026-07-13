using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string OrderNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal ShippingTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public Guid? AddressId { get; private set; }
    public Address? Address { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Order()
    {
    }

    public Order(
        Guid userId,
        string orderNumber,
        decimal subTotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal,
        Guid? addressId = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number cannot be empty.");
        }

        ValidateTotals(subTotal, discountTotal, shippingTotal, taxTotal, grandTotal);

        UserId = userId;
        OrderNumber = orderNumber.Trim();
        Status = OrderStatus.Pending;
        SubTotal = subTotal;
        DiscountTotal = discountTotal;
        ShippingTotal = shippingTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
        AddressId = addressId == Guid.Empty ? throw new DomainException("Address id cannot be empty.") : addressId;
    }

    public void ChangeStatus(OrderStatus status)
    {
        Status = status;
        if (status == OrderStatus.Paid)
        {
            PaidAt = DateTime.UtcNow;
            CancelledAt = null;
        }

        if (status == OrderStatus.Cancelled)
        {
            CancelledAt = DateTime.UtcNow;
        }

        MarkAsUpdated();
    }

    private static void ValidateTotals(
        decimal subTotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal)
    {
        if (subTotal < 0 || discountTotal < 0 || shippingTotal < 0 || taxTotal < 0 || grandTotal < 0)
        {
            throw new DomainException("Order totals cannot be negative.");
        }
    }
}
