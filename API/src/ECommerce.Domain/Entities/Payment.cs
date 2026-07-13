using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Payment()
    {
    }

    public Payment(Guid orderId, PaymentProvider provider, decimal amount, PaymentStatus status = PaymentStatus.Pending)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        OrderId = orderId;
        Provider = provider;
        Amount = amount;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(string? transactionId = null)
    {
        if (Status == PaymentStatus.Refunded || Status == PaymentStatus.Cancelled)
        {
            throw new DomainException("Refunded or cancelled payment cannot be marked as paid.");
        }

        Status = PaymentStatus.Paid;
        TransactionId = transactionId?.Trim();
        FailureReason = null;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string failureReason, string? transactionId = null)
    {
        if (Status == PaymentStatus.Paid || Status == PaymentStatus.Refunded)
        {
            throw new DomainException("Paid or refunded payment cannot be marked as failed.");
        }

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new DomainException("Failure reason cannot be empty.");
        }

        Status = PaymentStatus.Failed;
        FailureReason = failureReason.Trim();
        TransactionId = transactionId?.Trim();
    }

    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Paid)
        {
            throw new DomainException("Only paid payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
    }

    public void Cancel()
    {
        if (Status == PaymentStatus.Paid || Status == PaymentStatus.Refunded)
        {
            throw new DomainException("Paid or refunded payment cannot be cancelled.");
        }

        Status = PaymentStatus.Cancelled;
    }
}
