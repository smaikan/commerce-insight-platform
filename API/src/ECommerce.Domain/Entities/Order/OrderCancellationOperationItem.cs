using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class OrderCancellationOperationItem : BaseEntity
{
    public const int MaximumProviderConversationIdLength = 100;
    public const int MaximumProviderTransactionIdLength = 150;
    public const int MaximumErrorCodeLength = 100;

    public Guid OperationId { get; private set; }
    public OrderCancellationOperation Operation { get; private set; } = null!;
    public Guid PaymentItemTransactionId { get; private set; }
    public PaymentItemTransaction PaymentItemTransaction { get; private set; } = null!;
    public string ProviderPaymentTransactionId { get; private set; } = null!;
    public string ProviderConversationId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public PaymentReversalItemStatus Status { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Burada EF Core'un item-level refund kaydını materialize edebilmesi için boş kurucuyu tutuyorum.
    private OrderCancellationOperationItem()
    {
    }

    // Burada standart refund isteğini gerçek provider transaction ve dağıtılmış tahsilat tutarıyla oluşturuyorum.
    internal OrderCancellationOperationItem(
        OrderCancellationOperation operation,
        PaymentItemTransaction paymentItem,
        DateTime createdAtUtc)
    {
        Operation = operation ?? throw new DomainException("Cancellation operation is required.");
        PaymentItemTransaction = paymentItem ?? throw new DomainException("Payment item transaction is required.");
        OperationId = operation.Id;
        PaymentItemTransactionId = paymentItem.Id;
        ProviderPaymentTransactionId = paymentItem.ProviderTransactionId;
        ProviderConversationId = $"refund-{Id:N}";
        Amount = paymentItem.PaidPrice;
        Status = PaymentReversalItemStatus.Pending;
        CreatedAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
    }

    // Burada refund kalemini provider çağrısı öncesinde işleniyor durumuna taşıyorum.
    internal void MarkProcessing(DateTime utcNow)
    {
        if (Status is not PaymentReversalItemStatus.Pending and
            not PaymentReversalItemStatus.ReconciliationPending and
            not PaymentReversalItemStatus.Processing)
        {
            throw new DomainException("Only a pending refund item can be processed.");
        }

        Status = PaymentReversalItemStatus.Processing;
        UpdatedAt = utcNow;
        ErrorCode = null;
    }

    // Burada doğrulanmış provider refund sonucunu item-level audit kaydında tamamlıyorum.
    public void MarkCompleted(DateTime utcNow)
    {
        if (Status == PaymentReversalItemStatus.Completed)
        {
            return;
        }

        Status = PaymentReversalItemStatus.Completed;
        CompletedAt = utcNow;
        UpdatedAt = utcNow;
        ErrorCode = null;
    }

    // Burada belirsiz refund sonucunu tekrar göndermeden önce reporting ile uzlaştırılacak durumda tutuyorum.
    public void MarkReconciliationPending(DateTime utcNow, string? errorCode)
    {
        if (Status != PaymentReversalItemStatus.Processing)
        {
            throw new DomainException("Only a processing refund item can await reconciliation.");
        }

        Status = PaymentReversalItemStatus.ReconciliationPending;
        UpdatedAt = utcNow;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode)
            ? null
            : errorCode.Trim()[..Math.Min(errorCode.Trim().Length, MaximumErrorCodeLength)];
    }

    // Burada kesin refund reddini aynı item için yeniden çağrı yapılmayacak terminal duruma alıyorum.
    public void MarkFailed(DateTime utcNow, string? errorCode)
    {
        Status = PaymentReversalItemStatus.Failed;
        UpdatedAt = utcNow;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode)
            ? null
            : errorCode.Trim()[..Math.Min(errorCode.Trim().Length, MaximumErrorCodeLength)];
    }
}
