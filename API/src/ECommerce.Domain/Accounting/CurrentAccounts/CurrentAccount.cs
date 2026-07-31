using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.CurrentAccounts;

public enum CurrentAccountType
{
    Customer = 1,
    Supplier = 2,
    CustomerAndSupplier = 3
}

public enum CurrentAccountTransactionType
{
    SupplierDebt = 1,
    SupplierDebtReversal = 2,
    SupplierPayment = 3,
    SupplierPaymentReversal = 4,
    CustomerReceivable = 10,
    CustomerReceivableReversal = 11,
    CustomerCollection = 12,
    CustomerCollectionReversal = 13
}

public sealed class CurrentAccount : AuditableEntity
{
    public const int MaximumCodeLength = 50;
    public const int MaximumNameLength = 250;
    public const int MaximumTradeNameLength = 250;
    public const int MaximumIdentityNumberLength = 20;
    public const int MaximumTaxOfficeLength = 100;
    public const int MaximumPhoneLength = 30;
    public const int MaximumEmailLength = 320;
    public const int MaximumAddressPartLength = 150;
    public const int MaximumAddressLineLength = 500;
    public const int MaximumPostalCodeLength = 20;
    private readonly List<CurrentAccountTransaction> _transactions = [];

    public string Code { get; private set; } = null!;
    public CurrentAccountType Type { get; private set; }
    public string Name { get; private set; } = null!;
    public string? TradeName { get; private set; }
    public string? NationalIdentityNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? AddressLine { get; private set; }
    public string? PostalCode { get; private set; }
    public bool IsActive { get; private set; }
    public long? UserId { get; private set; }
    public IReadOnlyCollection<CurrentAccountTransaction> Transactions => _transactions.AsReadOnly();

    // Burada EF Core'un cari hesabı veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private CurrentAccount()
    {
    }

    // Burada müşteri, tedarikçi veya ortak tarafı bütün temel iletişim ve adres bilgileriyle oluşturuyorum.
    public CurrentAccount(
        string code,
        CurrentAccountType type,
        string name,
        string? tradeName,
        string? nationalIdentityNumber,
        string? taxNumber,
        string? taxOffice,
        string? phoneNumber,
        string? email,
        string? country,
        string? city,
        string? district,
        string? neighborhood,
        string? addressLine,
        string? postalCode,
        long? userId = null)
    {
        SetMasterData(
            code,
            type,
            name,
            tradeName,
            nationalIdentityNumber,
            taxNumber,
            taxOffice,
            phoneNumber,
            email,
            country,
            city,
            district,
            neighborhood,
            addressLine,
            postalCode,
            userId);
        IsActive = true;
    }

    // Burada tek master kaydındaki kimlik, iletişim, vergi ve adres bilgilerini birlikte güncelliyorum.
    public void Update(
        string code,
        CurrentAccountType type,
        string name,
        string? tradeName,
        string? nationalIdentityNumber,
        string? taxNumber,
        string? taxOffice,
        string? phoneNumber,
        string? email,
        string? country,
        string? city,
        string? district,
        string? neighborhood,
        string? addressLine,
        string? postalCode,
        long? userId = null)
    {
        SetMasterData(
            code,
            type,
            name,
            tradeName,
            nationalIdentityNumber,
            taxNumber,
            taxOffice,
            phoneNumber,
            email,
            country,
            city,
            district,
            neighborhood,
            addressLine,
            postalCode,
            userId);
        MarkAsUpdated();
    }

    // Burada cari hesabı yeni fatura seçimlerine kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada cari hesabı yeni fatura seçimlerine yeniden açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada cari hesabın alış faturasında tedarikçi olarak kullanılabildiğini bildiriyorum.
    public bool CanBeSupplier()
    {
        return IsActive && Type is CurrentAccountType.Supplier or CurrentAccountType.CustomerAndSupplier;
    }

    // Burada cari hesabın satış faturasında müşteri olarak kullanılabildiğini bildiriyorum.
    public bool CanBeCustomer()
    {
        return IsActive && Type is CurrentAccountType.Customer or CurrentAccountType.CustomerAndSupplier;
    }

    // Burada kaynak belgeye bağlı değişmez debit veya credit hareketini doğrudan cari hesaba ekliyorum.
    public CurrentAccountTransaction AddTransaction(
        CurrentAccountTransactionType type,
        decimal debitAmount,
        decimal creditAmount,
        string currencyCode,
        decimal exchangeRate,
        DateTime transactionDate,
        DateTime? dueDate,
        AccountingSourceType sourceType,
        Guid sourceId,
        string? description)
    {
        if (_transactions.Any(item => item.SourceType == sourceType &&
                                      item.SourceId == sourceId &&
                                      item.Type == type))
        {
            throw new DomainException("The accounting source transaction already exists.");
        }

        var transaction = new CurrentAccountTransaction(
            this,
            type,
            debitAmount,
            creditAmount,
            currencyCode,
            exchangeRate,
            transactionDate,
            dueDate,
            sourceType,
            sourceId,
            description);
        _transactions.Add(transaction);
        MarkAsUpdated();
        return transaction;
    }

    // Burada cari hesap master alanlarını tek doğrulama noktasından uyguluyorum.
    private void SetMasterData(
        string code,
        CurrentAccountType type,
        string name,
        string? tradeName,
        string? nationalIdentityNumber,
        string? taxNumber,
        string? taxOffice,
        string? phoneNumber,
        string? email,
        string? country,
        string? city,
        string? district,
        string? neighborhood,
        string? addressLine,
        string? postalCode,
        long? userId)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Current account type is invalid.");
        }

        if (userId is <= 0)
        {
            throw new DomainException("User id must be positive when supplied.");
        }

        if (userId.HasValue && type == CurrentAccountType.Supplier)
        {
            throw new DomainException("A user-linked current account must support the customer role.");
        }

        Code = NormalizeRequired(code, MaximumCodeLength, "Current account code").ToUpperInvariant();
        Type = type;
        Name = NormalizeRequired(name, MaximumNameLength, "Current account name");
        TradeName = NormalizeOptional(tradeName, MaximumTradeNameLength, "Trade name");
        NationalIdentityNumber = NormalizeOptional(nationalIdentityNumber, MaximumIdentityNumberLength, "National identity number");
        TaxNumber = NormalizeOptional(taxNumber, MaximumIdentityNumberLength, "Tax number");
        TaxOffice = NormalizeOptional(taxOffice, MaximumTaxOfficeLength, "Tax office");
        PhoneNumber = NormalizeOptional(phoneNumber, MaximumPhoneLength, "Phone number");
        Email = NormalizeEmail(email);
        Country = NormalizeOptional(country, MaximumAddressPartLength, "Country");
        City = NormalizeOptional(city, MaximumAddressPartLength, "City");
        District = NormalizeOptional(district, MaximumAddressPartLength, "District");
        Neighborhood = NormalizeOptional(neighborhood, MaximumAddressPartLength, "Neighborhood");
        AddressLine = NormalizeOptional(addressLine, MaximumAddressLineLength, "Address line");
        PostalCode = NormalizeOptional(postalCode, MaximumPostalCodeLength, "Postal code");
        UserId = userId;
    }

    // Burada zorunlu cari hesap metinlerini temizleyip uzunluk sınırını koruyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} cannot be empty.");
    }

    // Burada isteğe bağlı cari hesap metnini boş veya güvenli uzunlukta saklıyorum.
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

    // Burada isteğe bağlı e-posta adresini temel biçim ve uzunluk kurallarıyla normalize ediyorum.
    private static string? NormalizeEmail(string? email)
    {
        var normalized = NormalizeOptional(email, MaximumEmailLength, "Email");
        if (normalized is not null && (!normalized.Contains('@') || normalized.StartsWith('@') || normalized.EndsWith('@')))
        {
            throw new DomainException("Email format is invalid.");
        }

        return normalized?.ToLowerInvariant();
    }
}

public sealed class CurrentAccountTransaction : BaseEntity
{
    public const int MaximumDescriptionLength = 500;

    public Guid CurrentAccountId { get; private set; }
    public CurrentAccount CurrentAccount { get; private set; } = null!;
    public CurrentAccountTransactionType Type { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal ExchangeRate { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public AccountingSourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un cari hareketi veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private CurrentAccountTransaction()
    {
    }

    // Burada tek yönlü hareketi para birimi, kur ve kaynak belgeyle birlikte değişmez kaydediyorum.
    internal CurrentAccountTransaction(
        CurrentAccount account,
        CurrentAccountTransactionType type,
        decimal debitAmount,
        decimal creditAmount,
        string currencyCode,
        decimal exchangeRate,
        DateTime transactionDate,
        DateTime? dueDate,
        AccountingSourceType sourceType,
        Guid sourceId,
        string? description)
    {
        if (account is null || account.Id == Guid.Empty || sourceId == Guid.Empty)
        {
            throw new DomainException("Current account and source ids are required.");
        }

        if ((debitAmount > 0m) == (creditAmount > 0m) || debitAmount < 0m || creditAmount < 0m)
        {
            throw new DomainException("A current account transaction must contain exactly one positive debit or credit amount.");
        }

        if (exchangeRate <= 0m || transactionDate == default)
        {
            throw new DomainException("Positive exchange rate and transaction date are required.");
        }

        CurrentAccountId = account.Id;
        CurrentAccount = account;
        Type = type;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        CurrencyCode = NormalizeCurrency(currencyCode);
        ExchangeRate = exchangeRate;
        TransactionDate = transactionDate;
        DueDate = dueDate;
        SourceType = sourceType;
        SourceId = sourceId;
        Description = NormalizeDescription(description);
        CreatedAt = DateTime.UtcNow;
    }

    // Burada işlem para birimini üç harfli kanonik koda dönüştürüyorum.
    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            throw new DomainException("Currency code must contain three letters.");
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency code must contain only ASCII letters.");
        }

        return normalized;
    }

    // Burada isteğe bağlı cari hareket açıklamasını güvenli uzunlukta saklıyorum.
    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        if (normalized.Length > MaximumDescriptionLength)
        {
            throw new DomainException($"Description cannot exceed {MaximumDescriptionLength} characters.");
        }

        return normalized;
    }
}
