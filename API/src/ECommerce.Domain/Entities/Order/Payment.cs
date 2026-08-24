using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public const int MaximumIdempotencyKeyLength = 80;
    public const int MaximumTransactionIdLength = 150;
    public const int MaximumFailureReasonLength = 500;
    public const int MaximumProviderTokenLength = 500;
    public const int MaximumConversationIdLength = 100;
    public const int MaximumPaymentPageUrlLength = 1000;
    public const string IdempotencyKeyPattern = "^[A-Za-z0-9_-]+$";

    private readonly List<PaymentItemTransaction> _itemTransactions = [];

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? ProviderToken { get; private set; }
    public string? ProviderConversationId { get; private set; }
    public string? PaymentPageUrl { get; private set; }
    public DateTime? ProviderTokenExpiresAt { get; private set; }
    public int? FraudStatus { get; private set; }
    public decimal? ProviderPaidAmount { get; private set; }
    public int? InstallmentCount { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CustomerAbandonedAt { get; private set; }
    public DateTime? AbandonmentNextReconciliationAt { get; private set; }
    public DateTime? AbandonmentReconciledAt { get; private set; }
    public DateTime? LateChargeReversedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<PaymentItemTransaction> ItemTransactions => _itemTransactions.AsReadOnly();

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

    // Burada iyzico CheckoutForm oturumunu yalnız bekleyen ödeme denemesine bağlayıp tekrar çağrılabilir hale getiriyorum.
    public void InitializeCheckoutForm(
        string providerToken,
        string conversationId,
        string paymentPageUrl,
        DateTime tokenExpiresAt)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Only pending payment can receive a checkout form session.");
        }

        if (tokenExpiresAt <= DateTime.UtcNow)
        {
            throw new DomainException("Checkout form expiration must be in the future.");
        }

        ProviderToken = NormalizeRequiredValue(providerToken, MaximumProviderTokenLength, "Provider token");
        ProviderConversationId = NormalizeRequiredValue(
            conversationId,
            MaximumConversationIdLength,
            "Provider conversation id");
        PaymentPageUrl = NormalizeAbsoluteHttpUrl(paymentPageUrl);
        ProviderTokenExpiresAt = tokenExpiresAt;
    }

    // Burada kesin başarısız CheckoutForm yanıtının doğrulanmış token ve conversation kimliğini denetim için saklıyorum.
    public void RecordCheckoutFormIdentity(string providerToken, string conversationId)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Only pending payment can receive checkout form identity.");
        }

        var normalizedToken = NormalizeRequiredValue(
            providerToken,
            MaximumProviderTokenLength,
            "Provider token");
        var normalizedConversationId = NormalizeRequiredValue(
            conversationId,
            MaximumConversationIdLength,
            "Provider conversation id");
        if ((ProviderToken is not null && ProviderToken != normalizedToken) ||
            (ProviderConversationId is not null && ProviderConversationId != normalizedConversationId))
        {
            throw new DomainException("Checkout form identity cannot be changed.");
        }

        ProviderToken = normalizedToken;
        ProviderConversationId = normalizedConversationId;
    }

    // Burada yalnız bekleyen ödeme denemesini sağlayıcının kesin tahsilat ayrıntılarıyla başarılı işaretliyorum.
    public void MarkAsPaid(
        string transactionId,
        int? fraudStatus = null,
        decimal? providerPaidAmount = null,
        int? installmentCount = null)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Only pending payment can be marked as paid.");
        }

        if (providerPaidAmount.HasValue != installmentCount.HasValue)
        {
            throw new DomainException("Provider paid amount and installment count must be recorded together.");
        }

        if (providerPaidAmount is <= 0)
        {
            throw new DomainException("Provider paid amount must be greater than zero.");
        }

        if (installmentCount is < 1 or > 12)
        {
            throw new DomainException("Installment count must be between 1 and 12.");
        }

        TransactionId = NormalizeRequiredValue(transactionId, MaximumTransactionIdLength, "Payment transaction id");

        Status = PaymentStatus.Paid;
        FailureReason = null;
        PaidAt = DateTime.UtcNow;
        FraudStatus = fraudStatus;
        ProviderPaidAmount = providerPaidAmount;
        InstallmentCount = installmentCount;
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

    // Burada sağlayıcının henüz kesinleştirmediği fraud durumunu ödeme beklemede kalırken kaydediyorum.
    public void RecordFraudStatus(int fraudStatus)
    {
        if (Status != PaymentStatus.Pending || fraudStatus is < -1 or > 1)
        {
            throw new DomainException("Fraud status can only be recorded for a pending payment.");
        }

        FraudStatus = fraudStatus;
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

    // Burada aynı gün provider cancel başarısından sonra tahsil edilmiş ödemeyi iptal edildi olarak kaydediyorum.
    public void MarkAsCancelledAfterProviderReversal()
    {
        if (Status != PaymentStatus.Paid)
        {
            throw new DomainException("Only a paid payment can be cancelled by the provider.");
        }

        Status = PaymentStatus.Cancelled;
    }

    // Burada CF-Retrieve sonucundaki gerçek item transaction ve paidPrice dağılımını yalnız bir kez kalıcılaştırıyorum.
    public IReadOnlyList<PaymentItemTransaction> RecordProviderItemTransactions(
        IReadOnlyCollection<ProviderPaymentItemSnapshot> items,
        DateTime utcNow)
    {
        if (Status != PaymentStatus.Paid || !ProviderPaidAmount.HasValue)
        {
            throw new DomainException("Only a paid payment can record provider item transactions.");
        }

        if (_itemTransactions.Count != 0)
        {
            return [];
        }

        if (items.Count == 0 ||
            items.Select(item => item.OrderItemId).Distinct().Count() != items.Count ||
            items.Select(item => item.ProviderTransactionId).Distinct(StringComparer.Ordinal).Count() != items.Count ||
            items.Sum(item => item.PaidPrice) != ProviderPaidAmount.Value)
        {
            throw new DomainException("Provider item transactions do not match the paid amount.");
        }

        var created = items
            .Select(item => new PaymentItemTransaction(
                this,
                item.OrderItemId,
                item.ProviderTransactionId,
                item.Price,
                item.PaidPrice,
                utcNow))
            .ToList();
        _itemTransactions.AddRange(created);
        return created;
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

    // Burada müşterinin açık CheckoutForm oturumunu terk etmesini izlenebilir ve sonradan uzlaştırılabilir biçimde kaydediyorum.
    public void AbandonCheckoutForm(DateTime utcNow)
    {
        if (Status != PaymentStatus.Pending || string.IsNullOrWhiteSpace(ProviderToken))
        {
            throw new DomainException("Only an initialized pending checkout form can be abandoned.");
        }

        Status = PaymentStatus.Cancelled;
        FailureReason = "Payment form was cancelled by the customer before completion.";
        CustomerAbandonedAt = utcNow;
        AbandonmentNextReconciliationAt = utcNow;
        AbandonmentReconciledAt = null;
        LateChargeReversedAt = null;
    }

    // Burada aynı terk edilmiş ödeme için paralel worker ve callback çağrılarını kısa bir veritabanı lease'iyle tekilleştiriyorum.
    public bool ClaimAbandonmentReconciliation(DateTime utcNow, TimeSpan leaseDuration)
    {
        if (Status != PaymentStatus.Cancelled || !CustomerAbandonedAt.HasValue ||
            AbandonmentReconciledAt.HasValue || string.IsNullOrWhiteSpace(ProviderToken) ||
            (AbandonmentNextReconciliationAt.HasValue && AbandonmentNextReconciliationAt.Value > utcNow))
        {
            return false;
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new DomainException("Abandonment reconciliation lease must be positive.");
        }

        AbandonmentNextReconciliationAt = utcNow.Add(leaseDuration);
        return true;
    }

    // Burada sağlayıcı sonucu hâlâ beklemedeyse terk edilmiş oturumu bounded aralıkla yeniden sorgulanabilir tutuyorum.
    public void ScheduleAbandonmentReconciliation(DateTime nextAttemptAt)
    {
        if (Status != PaymentStatus.Cancelled || !CustomerAbandonedAt.HasValue || AbandonmentReconciledAt.HasValue)
        {
            throw new DomainException("Only an open abandoned checkout form can be rescheduled.");
        }

        AbandonmentNextReconciliationAt = nextAttemptAt;
    }

    // Burada tahsilat oluşmadan kapanan terk edilmiş oturumun izleme döngüsünü terminal olarak tamamlıyorum.
    public void CompleteAbandonmentReconciliation(DateTime utcNow)
    {
        if (Status != PaymentStatus.Cancelled || !CustomerAbandonedAt.HasValue)
        {
            throw new DomainException("Only an abandoned checkout form can complete reconciliation.");
        }

        AbandonmentReconciledAt = utcNow;
        AbandonmentNextReconciliationAt = null;
    }

    // Burada iptal edilmiş siparişe geç ulaşan tahsilatın iyzico'da geri çevrildiğini ödeme denetim kaydına işliyorum.
    public void RecordReversedLateCharge(
        string transactionId,
        int? fraudStatus,
        decimal providerPaidAmount,
        int installmentCount,
        DateTime utcNow)
    {
        if (Status != PaymentStatus.Cancelled || !CustomerAbandonedAt.HasValue)
        {
            throw new DomainException("Only an abandoned checkout form can record a reversed late charge.");
        }

        if (providerPaidAmount <= 0 || installmentCount is < 1 or > 12)
        {
            throw new DomainException("Late charge provider details are invalid.");
        }

        TransactionId = NormalizeRequiredValue(transactionId, MaximumTransactionIdLength, "Payment transaction id");
        FraudStatus = fraudStatus;
        ProviderPaidAmount = providerPaidAmount;
        InstallmentCount = installmentCount;
        FailureReason = "Late provider charge was automatically cancelled after customer abandonment.";
        LateChargeReversedAt = utcNow;
        AbandonmentReconciledAt = utcNow;
        AbandonmentNextReconciliationAt = null;
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

    // Burada ödeme sayfası adresini yalnız mutlak HTTP veya HTTPS kabul ederek saklıyorum.
    private static string NormalizeAbsoluteHttpUrl(string value)
    {
        var normalizedValue = NormalizeRequiredValue(value, MaximumPaymentPageUrlLength, "Payment page URL");
        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException("Payment page URL must be an absolute HTTP or HTTPS URL.");
        }

        return normalizedValue;
    }
}
