using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class ReturnRequest : AuditableEntity
{
    public const int MaximumItemCount = Order.MaximumItemCount;
    public const int MaximumReturnNumberLength = 30;
    public const int MaximumCustomerNoteLength = 1_000;
    public const int MaximumDecisionNoteLength = 1_000;

    private readonly List<ReturnItem> _items = [];

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public long UserId { get; private set; }
    public string ReturnNumber { get; private set; } = null!;
    public ReturnType Type { get; private set; }
    public ReturnRequestStatus Status { get; private set; }
    public decimal RefundTotal { get; private set; }
    public string? CustomerNote { get; private set; }
    public string? DecisionNote { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public IReadOnlyCollection<ReturnItem> Items => _items.AsReadOnly();
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Burada EF Core'un iade talebi aggregate'ını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ReturnRequest()
    {
    }

    // Burada teslim edilmiş siparişe bağlı iade veya değişim talebinin temel kurallarını kuruyorum.
    public ReturnRequest(
        Guid orderId,
        long userId,
        string returnNumber,
        ReturnType type,
        string? customerNote = null)
    {
        if (orderId == Guid.Empty || userId <= 0)
        {
            throw new DomainException("Order and user ids are required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Return request type is invalid.");
        }

        OrderId = orderId;
        UserId = userId;
        ReturnNumber = NormalizeReturnNumber(returnNumber);
        Type = type;
        Status = ReturnRequestStatus.Requested;
        CustomerNote = NormalizeOptionalNote(customerNote, MaximumCustomerNoteLength, "Customer note");
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada güvenilir sipariş snapshot'ından tekil bir iade kalemini talebe ekliyorum.
    public ReturnItem AddItem(
        OrderItem orderItem,
        int quantity,
        Guid? replacementProductVariantId = null,
        decimal? refundTotal = null)
    {
        if (orderItem is null || orderItem.OrderId != OrderId)
        {
            throw new DomainException("Return item must belong to the return request order.");
        }

        if (Status != ReturnRequestStatus.Requested)
        {
            throw new DomainException("Return items can only be added while the request is pending.");
        }

        if (_items.Count >= MaximumItemCount)
        {
            throw new DomainException($"Return request cannot contain more than {MaximumItemCount} items.");
        }

        if (_items.Any(item => item.OrderItemId == orderItem.Id))
        {
            throw new DomainException("An order item can only appear once in the same return request.");
        }

        if (quantity > orderItem.Quantity)
        {
            throw new DomainException("Return quantity cannot exceed the ordered quantity.");
        }

        var item = new ReturnItem(
            this,
            orderItem.Id,
            orderItem.ProductId,
            orderItem.ProductVariantId,
            orderItem.ProductTitleSnapshot,
            orderItem.VariantSkuSnapshot,
            orderItem.UnitPrice,
            quantity,
            replacementProductVariantId,
            Type == ReturnType.Refund
                ? refundTotal ?? CalculateDefaultRefundTotal(orderItem, quantity)
                : refundTotal);
        _items.Add(item);
        if (Type == ReturnType.Refund)
        {
            try
            {
                RefundTotal = checked(RefundTotal + item.RefundTotal);
            }
            catch (OverflowException exception)
            {
                throw new DomainException("Return refund total exceeds the supported monetary limit.", exception);
            }
        }

        RefreshConcurrencyToken();
        MarkAsUpdated();
        return item;
    }

    // Burada yalnız bekleyen ve kalem içeren iade talebini yöneticinin onayına taşıyorum.
    public void Approve(DateTime utcNow, string? decisionNote = null)
    {
        EnsureUtc(utcNow);
        EnsureHasItems();
        EnsureStatus(ReturnRequestStatus.Requested, "Only requested return requests can be approved.");
        Status = ReturnRequestStatus.Approved;
        DecisionNote = NormalizeOptionalNote(decisionNote, MaximumDecisionNoteLength, "Decision note");
        ApprovedAt = utcNow;
        RefreshConcurrencyToken();
        MarkAsUpdated();
    }

    // Burada yalnız bekleyen iade talebini gerekçesiyle reddediyorum.
    public void Reject(DateTime utcNow, string? decisionNote = null)
    {
        EnsureUtc(utcNow);
        EnsureStatus(ReturnRequestStatus.Requested, "Only requested return requests can be rejected.");
        Status = ReturnRequestStatus.Rejected;
        DecisionNote = NormalizeOptionalNote(decisionNote, MaximumDecisionNoteLength, "Decision note");
        RejectedAt = utcNow;
        RefreshConcurrencyToken();
        MarkAsUpdated();
    }

    // Burada yalnız onaylı talebi fiziksel ürünler teslim alındığında stok geri yükleme adımına geçiriyorum.
    public void Receive(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        EnsureStatus(ReturnRequestStatus.Approved, "Only approved return requests can be received.");
        Status = ReturnRequestStatus.Received;
        ReceivedAt = utcNow;
        RefreshConcurrencyToken();
        MarkAsUpdated();
    }

    // Burada teslim alınmış iade veya değişim talebinin mali ya da lojistik kapanışını bir kez tamamlıyorum.
    public void Complete(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        EnsureStatus(ReturnRequestStatus.Received, "Only received return requests can be completed.");
        Status = ReturnRequestStatus.Completed;
        CompletedAt = utcNow;
        RefreshConcurrencyToken();
        MarkAsUpdated();
    }

    // Burada talebin stok veya finansal etki oluşturabilecek aktif bir durumda olup olmadığını bildiriyorum.
    public bool ConsumesReturnQuantity()
    {
        return Status is not ReturnRequestStatus.Rejected;
    }

    // Burada talebin tamamlanmış bir geri ödeme olarak sipariş miktarına sayılıp sayılmayacağını bildiriyorum.
    public bool IsCompletedRefund()
    {
        return Type == ReturnType.Refund && Status == ReturnRequestStatus.Completed;
    }

    // Burada talebin en az bir kalem taşıdığını geçiş kurallarından önce doğruluyorum.
    private void EnsureHasItems()
    {
        if (_items.Count == 0)
        {
            throw new DomainException("Return request must contain at least one item.");
        }
    }

    // Burada durum geçişinin yalnız beklenen önceki durumdan yapılmasını sağlıyorum.
    private void EnsureStatus(ReturnRequestStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException(message);
        }
    }

    // Burada iş akışı zamanlarının UTC olmasını zorunlu tutuyorum.
    private static void EnsureUtc(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Return request time must be UTC.");
        }
    }

    // Burada isteğe bağlı notu boşluk ve uzunluk kurallarına göre normalize ediyorum.
    private static string? NormalizeOptionalNote(string? value, int maximumLength, string fieldName)
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

    // Burada iade numarasını boş olmayan, sınırlı ve değişmez takip değerine dönüştürüyorum.
    private static string NormalizeReturnNumber(string returnNumber)
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
        {
            throw new DomainException("Return number cannot be empty.");
        }

        var normalizedReturnNumber = returnNumber.Trim().ToUpperInvariant();
        if (normalizedReturnNumber.Length > MaximumReturnNumberLength)
        {
            throw new DomainException($"Return number cannot exceed {MaximumReturnNumberLength} characters.");
        }

        return normalizedReturnNumber;
    }

    // Burada doğrudan aggregate kullanımlarında kısmi iade tutarını sipariş kaleminin vergi ve indirim dahil toplamından oransal hesaplıyorum.
    private static decimal CalculateDefaultRefundTotal(OrderItem orderItem, int quantity)
    {
        return decimal.Round(
            orderItem.RefundTotal * quantity / orderItem.Quantity,
            OrderItem.SupportedPriceScale,
            MidpointRounding.AwayFromZero);
    }

    // Burada aynı iade talebinin eşzamanlı yönetici kararlarını ayırt etmek için yeni sürüm değeri üretiyorum.
    private void RefreshConcurrencyToken()
    {
        ConcurrencyToken = Guid.NewGuid();
    }
}
