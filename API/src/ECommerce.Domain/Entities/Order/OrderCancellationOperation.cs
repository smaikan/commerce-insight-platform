using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class OrderCancellationOperation : BaseEntity
{
    public const string ProviderResponseMismatchErrorCode = "provider_response_mismatch";
    public const int MaximumProviderVerificationAttempts = 3;
    public const int MaximumProviderConversationIdLength = 100;
    public const int MaximumProviderPaymentIdLength = 150;
    public const int MaximumErrorCodeLength = 100;
    public const int MaximumErrorSummaryLength = 500;

    private readonly List<OrderCancellationOperationItem> _items = [];

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid PaymentId { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public OrderCancellationInitiatorType InitiatorType { get; private set; }
    public OrderCancellationOperationStatus Status { get; private set; }
    public PaymentReversalType ReversalType { get; private set; }
    public string ProviderConversationId { get; private set; } = null!;
    public string ProviderPaymentId { get; private set; } = null!;
    public string? ErrorCode { get; private set; }
    public string? ErrorSummary { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public IReadOnlyCollection<OrderCancellationOperationItem> Items => _items.AsReadOnly();

    // Burada EF Core'un iptal operasyonunu materialize edebilmesi için boş kurucuyu tutuyorum.
    private OrderCancellationOperation()
    {
    }

    // Burada tahsil edilmiş sipariş için tek ve izlenebilir provider ters işlem niyetini oluşturuyorum.
    public OrderCancellationOperation(
        Order order,
        Payment payment,
        OrderCancellationInitiatorType initiatorType,
        PaymentReversalType reversalType,
        DateTime createdAtUtc)
    {
        if (order is null || payment is null || payment.OrderId != order.Id ||
            payment.Status != PaymentStatus.Paid ||
            string.IsNullOrWhiteSpace(payment.TransactionId))
        {
            throw new DomainException("A paid provider payment is required for order cancellation.");
        }

        EnsureUtc(createdAtUtc, "Cancellation operation creation time");
        if (!Enum.IsDefined(initiatorType) || !Enum.IsDefined(reversalType))
        {
            throw new DomainException("Cancellation initiator or reversal type is invalid.");
        }

        Order = order;
        OrderId = order.Id;
        Payment = payment;
        PaymentId = payment.Id;
        InitiatorType = initiatorType;
        Status = OrderCancellationOperationStatus.Requested;
        ReversalType = reversalType;
        ProviderConversationId = $"order-cancel-{Id:N}";
        ProviderPaymentId = payment.TransactionId;
        CreatedAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
        NextAttemptAt = createdAtUtc;
        ConcurrencyToken = Guid.NewGuid();

        if (reversalType == PaymentReversalType.Refund)
        {
            EnsureRefundItems(payment.ItemTransactions, createdAtUtc);
        }
    }

    // Burada operasyonu kısa bir lease ile HTTP veya worker işlemcilerinden yalnız birine veriyorum.
    public bool TryClaim(DateTime utcNow, TimeSpan leaseDuration)
    {
        EnsureUtc(utcNow, "Cancellation operation claim time");
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new DomainException("Cancellation operation lease duration must be positive.");
        }

        if (Status is not OrderCancellationOperationStatus.Requested and
            not OrderCancellationOperationStatus.ReconciliationPending and
            not OrderCancellationOperationStatus.Processing ||
            NextAttemptAt.HasValue && NextAttemptAt.Value > utcNow)
        {
            return false;
        }

        Status = OrderCancellationOperationStatus.Processing;
        AttemptCount = checked(AttemptCount + 1);
        NextAttemptAt = utcNow.Add(leaseDuration);
        Touch(utcNow);
        return true;
    }

    // Burada aynı gün cancel kesin reddedildiğinde gerçek provider kalemleriyle standart refund'a geçiyorum.
    public void SwitchToRefund(IEnumerable<PaymentItemTransaction> paymentItems, DateTime utcNow)
    {
        EnsureProcessing();
        EnsureUtc(utcNow, "Cancellation operation refund switch time");
        ReversalType = PaymentReversalType.Refund;
        EnsureRefundItems(paymentItems, utcNow);
        Touch(utcNow);
    }

    // Burada sonucu belirsiz provider isteğini mutasyon yapmadan bounded reconciliation kuyruğunda tutuyorum.
    public void MarkReconciliationPending(
        DateTime utcNow,
        DateTime nextAttemptAtUtc,
        string? errorCode,
        string? errorSummary)
    {
        EnsureProcessing();
        EnsureUtc(utcNow, "Cancellation reconciliation time");
        EnsureUtc(nextAttemptAtUtc, "Cancellation next attempt time");
        if (nextAttemptAtUtc <= utcNow)
        {
            throw new DomainException("Cancellation next attempt time must be in the future.");
        }

        Status = OrderCancellationOperationStatus.ReconciliationPending;
        NextAttemptAt = nextAttemptAtUtc;
        ErrorCode = NormalizeOptional(errorCode, MaximumErrorCodeLength);
        ErrorSummary = NormalizeOptional(errorSummary, MaximumErrorSummaryLength);
        Touch(utcNow);
    }

    // Burada kesin provider reddini tekrar gönderilmeyecek terminal operasyona dönüştürüyorum.
    public void MarkFailed(DateTime utcNow, string? errorCode, string errorSummary)
    {
        EnsureProcessing();
        EnsureUtc(utcNow, "Cancellation failure time");
        Status = OrderCancellationOperationStatus.Failed;
        NextAttemptAt = null;
        ErrorCode = NormalizeOptional(errorCode, MaximumErrorCodeLength);
        ErrorSummary = NormalizeRequired(errorSummary, MaximumErrorSummaryLength);
        Touch(utcNow);
    }

    // Burada güvenle otomatikleştirilemeyen eski veya tutarsız ödemeyi operatör incelemesine bırakıyorum.
    public void MarkManualReview(DateTime utcNow, string errorCode, string errorSummary)
    {
        EnsureUtc(utcNow, "Cancellation manual review time");
        if (Status == OrderCancellationOperationStatus.Completed)
        {
            throw new DomainException("A completed cancellation cannot require manual review.");
        }

        Status = OrderCancellationOperationStatus.ManualReview;
        NextAttemptAt = null;
        ErrorCode = NormalizeRequired(errorCode, MaximumErrorCodeLength);
        ErrorSummary = NormalizeRequired(errorSummary, MaximumErrorSummaryLength);
        Touch(utcNow);
    }

    // Burada yalnız Application tarafından güvenli bulunan teknik incelemeyi yeniden provider mutabakatına açıyorum.
    public void RequeueManualReview(DateTime utcNow)
    {
        EnsureUtc(utcNow, "Cancellation manual review retry time");
        if (Status != OrderCancellationOperationStatus.ManualReview)
        {
            throw new DomainException("Only a manual-review cancellation can be requeued.");
        }

        Status = OrderCancellationOperationStatus.ReconciliationPending;
        NextAttemptAt = utcNow;
        ErrorCode = null;
        ErrorSummary = null;
        Touch(utcNow);
    }

    // Burada provider tarafından kesin geri alınan tahsilatı yerel etkilerle aynı transaction'da tamamlıyorum.
    public void MarkCompleted(PaymentReversalType reversalType, DateTime utcNow)
    {
        if (Status == OrderCancellationOperationStatus.Completed)
        {
            return;
        }

        EnsureUtc(utcNow, "Cancellation completion time");
        if (reversalType != ReversalType)
        {
            ReversalType = reversalType;
        }

        if (ReversalType == PaymentReversalType.Refund && _items.Any(item => item.Status != PaymentReversalItemStatus.Completed))
        {
            throw new DomainException("All provider items must be refunded before completing the cancellation.");
        }

        Status = OrderCancellationOperationStatus.Completed;
        CompletedAt = utcNow;
        NextAttemptAt = null;
        ErrorCode = null;
        ErrorSummary = null;
        Touch(utcNow);
    }

    // Burada refund kalemini provider çağrısından hemen önce lease altında işleniyor olarak işaretliyorum.
    public OrderCancellationOperationItem? ClaimNextRefundItem(DateTime utcNow)
    {
        EnsureProcessing();
        EnsureUtc(utcNow, "Refund item claim time");
        var item = _items
            .OrderBy(candidate => candidate.ProviderPaymentTransactionId, StringComparer.Ordinal)
            .FirstOrDefault(candidate => candidate.Status is PaymentReversalItemStatus.Pending or
                PaymentReversalItemStatus.ReconciliationPending or
                PaymentReversalItemStatus.Processing);
        item?.MarkProcessing(utcNow);
        if (item is not null)
        {
            Touch(utcNow);
        }

        return item;
    }

    // Burada gerekli ise refund kalemlerini yalnız provider'ın kalıcı gerçek dağılımından bir kez üretiyorum.
    private void EnsureRefundItems(IEnumerable<PaymentItemTransaction> paymentItems, DateTime utcNow)
    {
        if (_items.Count != 0)
        {
            return;
        }

        var items = paymentItems.OrderBy(item => item.ProviderTransactionId, StringComparer.Ordinal).ToList();
        if (items.Count == 0 || items.Sum(item => item.PaidPrice) != Payment.ProviderPaidAmount)
        {
            throw new DomainException("Complete provider item transactions are required for a standard refund.");
        }

        foreach (var paymentItem in items)
        {
            _items.Add(new OrderCancellationOperationItem(this, paymentItem, utcNow));
        }
    }

    // Burada yalnız aktif işlemci lease'inin operasyon durumunu değiştirmesine izin veriyorum.
    private void EnsureProcessing()
    {
        if (Status != OrderCancellationOperationStatus.Processing)
        {
            throw new DomainException("Only a processing cancellation operation can be updated.");
        }
    }

    // Burada her operasyon mutasyonunda zaman ve optimistic concurrency tokenını birlikte yeniliyorum.
    private void Touch(DateTime utcNow)
    {
        UpdatedAt = utcNow;
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada operasyon zaman damgalarının gerçekten UTC olmasını doğruluyorum.
    private static void EnsureUtc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }

    // Burada provider hata özetlerini boş olmayan ve sınırlı metne indiriyorum.
    private static string NormalizeRequired(string value, int maximumLength)
    {
        return NormalizeOptional(value, maximumLength)
            ?? throw new DomainException("Cancellation operation text cannot be empty.");
    }

    // Burada provider hata ayrıntılarının ham payload taşımadan kolon sınırında kalmasını sağlıyorum.
    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
