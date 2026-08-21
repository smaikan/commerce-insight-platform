namespace ECommerce.Domain.Enums;

public enum ContactReplyDeliveryStatus
{
    Queued = 0,
    Sent = 1,
    Retrying = 2,
    DeadLetter = 3
}
