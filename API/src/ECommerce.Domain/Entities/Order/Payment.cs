using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public const int MaximumIdempotencyKeyLength = 80;
    public const int MaximumTransactionIdLength = 150;
    public const int MaximumFailureReasonLength = 500;
    public const string IdempotencyKeyPattern = "^[A-Za-z0-9_-]+$";

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Payment()
    {
    }

    // Burada güvenli ödeme denemesini sipariş, sağlayıcı, tutar ve retry anahtarıyla oluşturuyorum.
    public Payment(
        Guid orderId,
        PaymentProvider provider,
        decimal amount,
        string? idempotencyKey = null,
        PaymentStatus status = PaymentStatus.Pending)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        if (!Enum.IsDefined(provider) || !Enum.IsDefined(status))
        {
            throw new DomainException("Payment provider or status is invalid.");
        }

        OrderId = orderId;
        Provider = provider;
        Amount = amount;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : NormalizeIdempotencyKey(idempotencyKey);
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada tekrar anahtarını uygulama ve SQL karşılaştırmalarıyla uyumlu, büyük harfli kanonik biçime getiriyorum.
    public static string NormalizeIdempotencyKey(string value)
    {
        var normalizedValue = NormalizeRequiredValue(
            value,
            MaximumIdempotencyKeyLength,
            "Payment idempotency key")
            .ToUpperInvariant();
        if (!normalizedValue.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '_' or '-'))
        {
            throw new DomainException("Payment idempotency key contains unsupported characters.");
        }

        return normalizedValue;
    }

    // Burada yalnız bekleyen ödeme denemesini sağlayıcı işlem kimliğiyle başarılı olarak işaretliyorum.
    public void MarkAsPaid(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Only pending payment can be marked as paid.");
        }

        TransactionId = NormalizeRequiredValue(transactionId, MaximumTransactionIdLength, "Payment transaction id");

        Status = PaymentStatus.Paid;
        FailureReason = null;
        PaidAt = DateTime.UtcNow;
    }

    // Burada yalnız bekleyen ödeme denemesini güvenli hata özetiyle başarısız olarak işaretliyorum.
    public void MarkAsFailed(string failureReason, string? transactionId = null)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Only pending payment can be marked as failed.");
        }

        Status = PaymentStatus.Failed;
        FailureReason = NormalizeRequiredValue(failureReason, MaximumFailureReasonLength, "Payment failure reason");
        TransactionId = NormalizeOptionalValue(transactionId, MaximumTransactionIdLength, "Payment transaction id");
    }

    // Burada sağlayıcı tarafından güvenle çözümlenmiş zaman aşımı denemesini tekrar işlenemeyecek başarısız duruma taşıyorum.
    public void MarkAsTimedOut()
    {
        MarkAsFailed("Payment attempt timed out after provider reconciliation.");
    }

    // Burada yalnız başarılı ödeme denemesini iade edildi olarak işaretliyorum.
    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Paid)
        {
            throw new DomainException("Only paid payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
    }

    // Burada henüz başarılı olmayan ödeme denemesini iptal ediyorum.
    public void Cancel()
    {
        if (Status == PaymentStatus.Paid || Status == PaymentStatus.Refunded)
        {
            throw new DomainException("Paid or refunded payment cannot be cancelled.");
        }

        Status = PaymentStatus.Cancelled;
    }

    // Burada zorunlu sağlayıcı metnini boşluk ve uzunluk kurallarına göre normalize ediyorum.
    private static string NormalizeRequiredValue(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return NormalizeOptionalValue(value, maximumLength, fieldName)!;
    }

    // Burada isteğe bağlı sağlayıcı metnini boşluk ve uzunluk kurallarına göre normalize ediyorum.
    private static string? NormalizeOptionalValue(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }
}
