namespace ECommerce.Domain.Enums;

public enum OrderCancellationOperationStatus
{
    Requested = 0,
    Processing = 1,
    ReconciliationPending = 2,
    Completed = 3,
    Failed = 4,
    ManualReview = 5
}
