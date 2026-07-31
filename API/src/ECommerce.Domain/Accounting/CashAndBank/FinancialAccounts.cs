using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.CashAndBank;

public enum FinancialTransactionType
{
    CustomerCollection = 1,
    SupplierPayment = 2,
    CashIn = 10,
    CashOut = 11,
    BankTransferIn = 20,
    BankTransferOut = 21,
    PosCollection = 30,
    BankCommission = 40,
    MarketplaceCommission = 41,
    Refund = 50,
    ReversalIn = 60,
    ReversalOut = 61
}

public enum FinancialTransactionDirection
{
    In = 1,
    Out = 2
}

public sealed class CashAccount : AuditableEntity
{
    public const int MaximumCodeLength = 50;
    public const int MaximumNameLength = 150;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = null!;
    public bool IsActive { get; private set; }

    // Burada EF Core'un kasa hesabını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private CashAccount()
    {
    }

    // Burada bakiyesi ledger'dan türetilecek aktif kasa hesabını oluşturuyorum.
    public CashAccount(string code, string name, string currencyCode)
    {
        Code = FinancialAccountRules.NormalizeRequired(code, MaximumCodeLength, "Cash account code").ToUpperInvariant();
        Name = FinancialAccountRules.NormalizeRequired(name, MaximumNameLength, "Cash account name");
        CurrencyCode = FinancialAccountRules.NormalizeCurrency(currencyCode);
        IsActive = true;
    }

    // Burada kasa hesabını yeni finansal işlemlere kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada kasa hesabını yeni finansal işlemlere açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}

public sealed class BankAccount : AuditableEntity
{
    public const int MaximumCodeLength = 50;
    public const int MaximumNameLength = 150;
    public const int MaximumBankNameLength = 150;
    public const int MaximumIbanLength = 34;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string BankName { get; private set; } = null!;
    public string? Iban { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public bool IsActive { get; private set; }

    // Burada EF Core'un banka hesabını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private BankAccount()
    {
    }

    // Burada bakiyesi ledger'dan türetilecek aktif banka hesabını oluşturuyorum.
    public BankAccount(string code, string name, string bankName, string? iban, string currencyCode)
    {
        Code = FinancialAccountRules.NormalizeRequired(code, MaximumCodeLength, "Bank account code").ToUpperInvariant();
        Name = FinancialAccountRules.NormalizeRequired(name, MaximumNameLength, "Bank account name");
        BankName = FinancialAccountRules.NormalizeRequired(bankName, MaximumBankNameLength, "Bank name");
        Iban = FinancialAccountRules.NormalizeOptional(iban, MaximumIbanLength, "IBAN")?.Replace(" ", string.Empty).ToUpperInvariant();
        CurrencyCode = FinancialAccountRules.NormalizeCurrency(currencyCode);
        IsActive = true;
    }

    // Burada banka hesabını yeni finansal işlemlere kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada banka hesabını yeni finansal işlemlere açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}

public sealed class FinancialTransaction : BaseEntity
{
    public const int MaximumDescriptionLength = 500;

    public Guid? CashAccountId { get; private set; }
    public CashAccount? CashAccount { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public BankAccount? BankAccount { get; private set; }
    public FinancialTransactionType Type { get; private set; }
    public FinancialTransactionDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public DateTime TransactionDate { get; private set; }
    public AccountingSourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid? ReversesTransactionId { get; private set; }
    public FinancialTransaction? ReversesTransaction { get; private set; }
    public string? Description { get; private set; }
    public long CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un finansal hareketi veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private FinancialTransaction()
    {
    }

    // Burada tek kasa veya banka hesabındaki değişmez giriş ya da çıkış hareketini oluşturuyorum.
    public FinancialTransaction(
        Guid? cashAccountId,
        Guid? bankAccountId,
        FinancialTransactionType type,
        decimal amount,
        string currencyCode,
        DateTime transactionDate,
        AccountingSourceType sourceType,
        Guid sourceId,
        long createdBy,
        string? description = null,
        Guid? reversesTransactionId = null)
    {
        if ((cashAccountId.HasValue ? 1 : 0) + (bankAccountId.HasValue ? 1 : 0) != 1)
        {
            throw new DomainException("Financial transaction must use exactly one cash or bank account.");
        }

        if (!Enum.IsDefined(type) || amount <= 0m || transactionDate == default || sourceId == Guid.Empty || createdBy <= 0)
        {
            throw new DomainException("Financial transaction type, positive amount, date, source and actor are required.");
        }

        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        Type = type;
        Direction = GetDirection(type);
        Amount = amount;
        CurrencyCode = FinancialAccountRules.NormalizeCurrency(currencyCode);
        TransactionDate = transactionDate;
        SourceType = sourceType;
        SourceId = sourceId;
        CreatedBy = createdBy;
        Description = FinancialAccountRules.NormalizeOptional(description, MaximumDescriptionLength, "Description");
        ReversesTransactionId = reversesTransactionId;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada finansal hareket türünün bakiyeye giriş mi çıkış mı yaptığını belirliyorum.
    private static FinancialTransactionDirection GetDirection(FinancialTransactionType type)
    {
        return type switch
        {
            FinancialTransactionType.CustomerCollection or
            FinancialTransactionType.CashIn or
            FinancialTransactionType.BankTransferIn or
            FinancialTransactionType.PosCollection => FinancialTransactionDirection.In,
            FinancialTransactionType.ReversalIn => FinancialTransactionDirection.In,
            _ => FinancialTransactionDirection.Out
        };
    }
}

internal static class FinancialAccountRules
{
    // Burada para birimini üç harfli kanonik koda dönüştürüyorum.
    internal static string NormalizeCurrency(string value)
    {
        var normalized = NormalizeRequired(value, 3, "Currency code").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency code must contain three ASCII letters.");
        }

        return normalized;
    }

    // Burada zorunlu finans hesabı metnini temizleyip uzunluk sınırını uyguluyorum.
    internal static string NormalizeRequired(string? value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} is required.");
    }

    // Burada isteğe bağlı finans hesabı metnini boş veya güvenli uzunlukta saklıyorum.
    internal static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
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
