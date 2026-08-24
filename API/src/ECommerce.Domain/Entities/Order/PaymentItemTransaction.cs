using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class PaymentItemTransaction : BaseEntity
{
    public const int MaximumProviderTransactionIdLength = 150;

    public Guid PaymentId { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public Guid OrderItemId { get; private set; }
    public OrderItem OrderItem { get; private set; } = null!;
    public string ProviderTransactionId { get; private set; } = null!;
    public decimal Price { get; private set; }
    public decimal PaidPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un kalıcı provider kalemini materialize edebilmesi için boş kurucuyu tutuyorum.
    private PaymentItemTransaction()
    {
    }

    // Burada iyzico'nun gerçek kalem dağılımını ödeme ve sipariş kalemiyle değişmez biçimde eşliyorum.
    internal PaymentItemTransaction(
        Payment payment,
        Guid orderItemId,
        string providerTransactionId,
        decimal price,
        decimal paidPrice,
        DateTime createdAtUtc)
    {
        if (payment is null || payment.Id == Guid.Empty || orderItemId == Guid.Empty)
        {
            throw new DomainException("Payment and order item ids are required for a provider item transaction.");
        }

        if (string.IsNullOrWhiteSpace(providerTransactionId) ||
            providerTransactionId.Trim().Length > MaximumProviderTransactionIdLength)
        {
            throw new DomainException("Provider item transaction id is invalid.");
        }

        if (price <= 0m || paidPrice <= 0m)
        {
            throw new DomainException("Provider item transaction amounts must be positive.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Provider item transaction creation time must be UTC.");
        }

        Payment = payment;
        PaymentId = payment.Id;
        OrderItemId = orderItemId;
        ProviderTransactionId = providerTransactionId.Trim();
        Price = price;
        PaidPrice = paidPrice;
        CreatedAt = createdAtUtc;
    }
}

public sealed record ProviderPaymentItemSnapshot(
    Guid OrderItemId,
    string ProviderTransactionId,
    decimal Price,
    decimal PaidPrice);
