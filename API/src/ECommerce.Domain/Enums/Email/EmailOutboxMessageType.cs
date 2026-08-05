namespace ECommerce.Domain.Enums;

public enum EmailOutboxMessageType
{
    PasswordReset = 1,
    Welcome = 2,
    OrderCreated = 3,
    PaymentPaid = 4,
    PaymentFailed = 5,
    OrderStatusChanged = 6,
    ReturnRequested = 7,
    ReturnStatusChanged = 8,
    GuestOrderAccess = 9
}
