namespace ECommerce.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Paid = 2,
    Preparing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}
