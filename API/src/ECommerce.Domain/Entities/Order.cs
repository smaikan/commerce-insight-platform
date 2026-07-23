using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public long UserId { get; private set; }
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
        long userId,
        string orderNumber,
        decimal subTotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal,
        Guid? addressId = null)
    {
        if (userId <= 0)
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

    public void ChangeStatus(OrderStatus status, DateTime utcNow)
    {
        if (!CanTransitionTo(status))
        {
            throw new DomainException($"Order status cannot change from {Status} to {status}.");
        }

        Status = status;
        if (status == OrderStatus.Paid)
        {
            PaidAt = utcNow;
        }

        if (status == OrderStatus.Cancelled)
        {
            CancelledAt = utcNow;
        }

        MarkAsUpdated();
    }

    private bool CanTransitionTo(OrderStatus targetStatus)
    {
        return Status switch
        {
            OrderStatus.Pending => targetStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => targetStatus is OrderStatus.Paid or OrderStatus.Cancelled,
            OrderStatus.Paid => targetStatus is OrderStatus.Preparing or OrderStatus.Refunded,
            OrderStatus.Preparing => targetStatus is OrderStatus.Shipped or OrderStatus.Refunded,
            OrderStatus.Shipped => targetStatus is OrderStatus.Delivered or OrderStatus.Refunded,
            OrderStatus.Delivered => targetStatus == OrderStatus.Refunded,
            OrderStatus.Cancelled or OrderStatus.Refunded => false,
            _ => false
        };
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

        if (discountTotal > subTotal)
        {
            throw new DomainException("Order discount total cannot exceed subtotal.");
        }

        var expectedGrandTotal = subTotal - discountTotal + shippingTotal + taxTotal;

        if (grandTotal != expectedGrandTotal)
        {
            throw new DomainException("Order grand total is not consistent with order totals.");
        }
    }
}
