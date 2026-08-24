namespace ECommerce.Domain.Enums;

public enum PaymentReversalItemStatus
{
    Pending = 0,
    Processing = 1,
    ReconciliationPending = 2,
    Completed = 3,
    Failed = 4
}
