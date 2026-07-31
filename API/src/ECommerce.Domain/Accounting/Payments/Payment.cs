using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Payments;

public enum PaymentType
{
    CustomerCollection = 1,
    SupplierPayment = 2
}

public enum PaymentDirection
{
    In = 1,
    Out = 2
}

public enum PaymentStatus
{
    Completed = 1,
    Cancelled = 2,
    Reversed = 3
}

public sealed class Payment : AuditableEntity
{
    public const int MaximumReferenceNumberLength = 100;
    public const int MaximumDescriptionLength = 500;
    public const int MaximumIdempotencyKeyLength = 100;
    private readonly List<PaymentAllocation> _allocations = [];

    public Guid CurrentAccountId { get; private set; }
    public CurrentAccount CurrentAccount { get; private set; } = null!;
    public PaymentType Type { get; private set; }
    public PaymentDirection Direction { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal ExchangeRate { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public Guid? CashAccountId { get; private set; }
    public CashAccount? CashAccount { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public BankAccount? BankAccount { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Description { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public long CreatedBy { get; private set; }
    public Guid? ReversesPaymentId { get; private set; }
    public Payment? ReversesPayment { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public long? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    // Burada EF Core'un ödeme kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private Payment()
    {
    }

    // Burada cari hesap ile tek kasa veya banka hesabına bağlı tamamlanmış ödemeyi oluşturuyorum.
    public Payment(
        CurrentAccount currentAccount,
        PaymentType type,
        decimal amount,
        string currencyCode,
        decimal exchangeRate,
        DateTime paymentDate,
        Guid? cashAccountId,
        Guid? bankAccountId,
        string idempotencyKey,
        long createdBy,
        string? referenceNumber = null,
        string? description = null,
        Guid? reversesPaymentId = null)
    {
        if (currentAccount is null || currentAccount.Id == Guid.Empty)
        {
            throw new DomainException("Current account is required.");
        }

        if (!Enum.IsDefined(type) || amount <= 0m || exchangeRate <= 0m || paymentDate == default)
        {
            throw new DomainException("Payment type, positive amount, exchange rate and date are required.");
        }

        if ((cashAccountId.HasValue ? 1 : 0) + (bankAccountId.HasValue ? 1 : 0) != 1)
        {
            throw new DomainException("Payment must use exactly one cash or bank account.");
        }

        if (createdBy <= 0)
        {
            throw new DomainException("Payment creator is required.");
        }

        CurrentAccountId = currentAccount.Id;
        CurrentAccount = currentAccount;
        Type = type;
        Direction = type == PaymentType.CustomerCollection ? PaymentDirection.In : PaymentDirection.Out;
        Status = reversesPaymentId.HasValue ? PaymentStatus.Reversed : PaymentStatus.Completed;
        Amount = amount;
        CurrencyCode = NormalizeCurrency(currencyCode);
        ExchangeRate = exchangeRate;
        PaymentDate = paymentDate;
        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        IdempotencyKey = NormalizeRequired(idempotencyKey, MaximumIdempotencyKeyLength, "Idempotency key");
        CreatedBy = createdBy;
        ReferenceNumber = NormalizeOptional(referenceNumber, MaximumReferenceNumberLength, "Reference number");
        Description = NormalizeOptional(description, MaximumDescriptionLength, "Description");
        ReversesPaymentId = reversesPaymentId;
    }

    // Burada ödeme tutarının henüz tahsis edilmemiş bölümünü geçerli tahsislerden hesaplıyorum.
    public decimal GetUnallocatedAmount()
    {
        return Amount - _allocations.Where(item => item.IsValid).Sum(item => item.AllocatedAmount);
    }

    // Burada geçerli cari borç veya alacak hareketine ödeme tahsisi ekliyorum.
    public PaymentAllocation Allocate(CurrentAccountTransaction transaction, decimal amount)
    {
        if (Status != PaymentStatus.Completed)
        {
            throw new DomainException("Cancelled or reversed payments cannot receive allocations.");
        }

        if (transaction is null || transaction.Id == Guid.Empty || transaction.CurrentAccountId != CurrentAccountId)
        {
            throw new DomainException("Allocation target must belong to the payment current account.");
        }

        if (amount <= 0m || amount > GetUnallocatedAmount())
        {
            throw new DomainException("Allocation exceeds the available payment amount.");
        }

        var expectedType = Type == PaymentType.CustomerCollection
            ? CurrentAccountTransactionType.CustomerReceivable
            : CurrentAccountTransactionType.SupplierDebt;
        if (transaction.Type != expectedType || !string.Equals(transaction.CurrencyCode, CurrencyCode, StringComparison.Ordinal))
        {
            throw new DomainException("Allocation target type or currency is not compatible with the payment.");
        }

        if (_allocations.Any(item => item.CurrentAccountTransactionId == transaction.Id && item.IsValid))
        {
            throw new DomainException("The payment already contains an allocation for this transaction.");
        }

        var allocation = new PaymentAllocation(this, transaction, amount);
        _allocations.Add(allocation);
        MarkAsUpdated();
        return allocation;
    }

    // Burada nihai ters kayıt akışı tamamlanana kadar ödeme iptal durumunu ve denetim bilgisini koruyorum.
    public void MarkCancelled(long cancelledBy, string reason)
    {
        if (Status != PaymentStatus.Completed || cancelledBy <= 0)
        {
            throw new DomainException("Only a completed payment can be cancelled by a valid actor.");
        }

        CancellationReason = NormalizeRequired(reason, MaximumDescriptionLength, "Cancellation reason");
        foreach (var allocation in _allocations.Where(item => item.IsValid))
        {
            allocation.Reverse();
        }

        Status = PaymentStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    // Burada para birimini üç harfli kanonik koda dönüştürüyorum.
    private static string NormalizeCurrency(string value)
    {
        var normalized = NormalizeRequired(value, 3, "Currency code").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency code must contain three ASCII letters.");
        }

        return normalized;
    }

    // Burada zorunlu ödeme metnini temizleyip uzunluk sınırını uyguluyorum.
    private static string NormalizeRequired(string? value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} is required.");
    }

    // Burada isteğe bağlı ödeme metnini boş veya güvenli uzunlukta saklıyorum.
    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }
}

public sealed class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public Guid CurrentAccountTransactionId { get; private set; }
    public CurrentAccountTransaction CurrentAccountTransaction { get; private set; } = null!;
    public decimal AllocatedAmount { get; private set; }
    public bool IsReversed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReversedAt { get; private set; }
    public bool IsValid => !IsReversed && Payment.Status == PaymentStatus.Completed;

    // Burada EF Core'un ödeme tahsisini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private PaymentAllocation()
    {
    }

    // Burada ödemeyi doğrudan muhasebe borç veya alacak hareketine tahsis ediyorum.
    internal PaymentAllocation(Payment payment, CurrentAccountTransaction transaction, decimal allocatedAmount)
    {
        if (payment is null || transaction is null || allocatedAmount <= 0m)
        {
            throw new DomainException("Payment, current account transaction and positive allocation are required.");
        }

        PaymentId = payment.Id;
        Payment = payment;
        CurrentAccountTransactionId = transaction.Id;
        CurrentAccountTransaction = transaction;
        AllocatedAmount = allocatedAmount;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada iptal veya ters kayıtta tahsisi fiziksel olarak silmeden geçersiz kılıyorum.
    internal void Reverse()
    {
        if (!IsReversed)
        {
            IsReversed = true;
            ReversedAt = DateTime.UtcNow;
        }
    }
}
