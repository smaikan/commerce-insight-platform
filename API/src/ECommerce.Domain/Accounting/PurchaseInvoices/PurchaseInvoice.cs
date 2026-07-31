using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.PurchaseInvoices;

public sealed class PurchaseInvoice : AuditableEntity
{
    public const int MaximumInvoiceNumberLength = 100;
    public const int MaximumDescriptionLength = 500;
    public const int MaximumAddressSnapshotLength = 1500;
    private readonly List<PurchaseInvoiceLine> _lines = [];

    public Guid CurrentAccountId { get; private set; }
    public CurrentAccount CurrentAccount { get; private set; } = null!;
    public string CurrentAccountNameSnapshot { get; private set; } = null!;
    public string? TaxNumberSnapshot { get; private set; }
    public string? TaxOfficeSnapshot { get; private set; }
    public string? PhoneNumberSnapshot { get; private set; }
    public string? EmailSnapshot { get; private set; }
    public string? AddressSnapshot { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal ExchangeRate { get; private set; }
    public DiscountType? InvoiceDiscountType { get; private set; }
    public decimal? InvoiceDiscountValue { get; private set; }
    public DiscountTaxBasis? InvoiceDiscountTaxBasis { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? Description { get; private set; }
    public long CreatedBy { get; private set; }
    public long? UpdatedBy { get; private set; }
    public long? PostedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public long? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public decimal SubtotalExcludingVat { get; private set; }
    public decimal SubtotalIncludingVat { get; private set; }
    public decimal LineDiscountTotalExcludingVat { get; private set; }
    public decimal LineDiscountTotalIncludingVat { get; private set; }
    public decimal InvoiceDiscountTotalExcludingVat { get; private set; }
    public decimal InvoiceDiscountTotalIncludingVat { get; private set; }
    public decimal TotalDiscountExcludingVat { get; private set; }
    public decimal TotalDiscountIncludingVat { get; private set; }
    public decimal NetAmountExcludingVat { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal GrandTotalIncludingVat { get; private set; }
    public decimal TotalAllocatedExpenseExcludingVat { get; private set; }
    public decimal TotalAllocatedExpenseIncludingVat { get; private set; }
    public decimal TotalFinalCostExcludingVat { get; private set; }
    public decimal TotalFinalCostIncludingVat { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public IReadOnlyCollection<PurchaseInvoiceLine> Lines => _lines.AsReadOnly();

    // Burada EF Core'un faturayı veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private PurchaseInvoice()
    {
    }

    // Burada fiziksel stoğa dokunmayan taslak alış faturası başlığını oluşturuyorum.
    public PurchaseInvoice(
        CurrentAccount currentAccount,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime? dueDate,
        string currencyCode,
        decimal exchangeRate,
        DiscountType? invoiceDiscountType,
        decimal? invoiceDiscountValue,
        DiscountTaxBasis? invoiceDiscountTaxBasis,
        string? description,
        long createdBy)
    {
        if (currentAccount is null || !currentAccount.CanBeSupplier() || createdBy <= 0)
        {
            throw new DomainException("An active supplier current account and creator are required.");
        }

        CurrentAccountId = currentAccount.Id;
        CurrentAccount = currentAccount;
        Status = InvoiceStatus.Draft;
        CaptureCurrentAccountSnapshot(currentAccount);
        InvoiceNumber = NormalizeRequired(invoiceNumber, MaximumInvoiceNumberLength, "Invoice number");
        InvoiceDate = EnsureDate(invoiceDate, "Invoice date");
        DueDate = dueDate;
        SetCurrency(currencyCode, exchangeRate);
        SetInvoiceDiscount(invoiceDiscountType, invoiceDiscountValue, invoiceDiscountTaxBasis);
        Description = NormalizeOptional(description, MaximumDescriptionLength);
        CreatedBy = createdBy;
    }

    // Burada yalnız taslak faturanın başlık girdilerini ve denetim aktörünü güncelliyorum.
    public void UpdateHeader(
        CurrentAccount currentAccount,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime? dueDate,
        string currencyCode,
        decimal exchangeRate,
        DiscountType? invoiceDiscountType,
        decimal? invoiceDiscountValue,
        DiscountTaxBasis? invoiceDiscountTaxBasis,
        string? description,
        long updatedBy)
    {
        EnsureDraft();
        if (currentAccount is null || !currentAccount.CanBeSupplier() || updatedBy <= 0)
        {
            throw new DomainException("An active supplier current account and updater are required.");
        }

        CurrentAccountId = currentAccount.Id;
        CurrentAccount = currentAccount;
        CaptureCurrentAccountSnapshot(currentAccount);
        InvoiceNumber = NormalizeRequired(invoiceNumber, MaximumInvoiceNumberLength, "Invoice number");
        InvoiceDate = EnsureDate(invoiceDate, "Invoice date");
        DueDate = dueDate;
        SetCurrency(currencyCode, exchangeRate);
        SetInvoiceDiscount(invoiceDiscountType, invoiceDiscountValue, invoiceDiscountTaxBasis);
        Description = NormalizeOptional(description, MaximumDescriptionLength);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada hesaplanmış satırı taslak faturaya ekliyorum.
    public void AddLine(PurchaseInvoiceLine line, long updatedBy)
    {
        EnsureDraft();
        if (line is null || line.PurchaseInvoiceId != Id || updatedBy <= 0)
        {
            throw new DomainException("A valid invoice line and updater are required.");
        }

        if (_lines.Any(item => item.LineNumber == line.LineNumber))
        {
            throw new DomainException("Invoice line numbers must be unique.");
        }

        _lines.Add(line);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada taslak faturadan henüz maliyet katmanı üretmemiş satırı kaldırıyorum.
    public void RemoveLine(Guid lineId, long updatedBy)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(item => item.Id == lineId)
            ?? throw new DomainException("Invoice line was not found.");
        _lines.Remove(line);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada ortak hesap motorundan gelen satır toplamlarını fatura başlığında birebir saklıyorum.
    // Burada yerinde değiştirilen mevcut satırın bu taslak faturaya ait olduğunu doğrulayıp audit bilgisini yeniliyorum.
    public void MarkLineUpdated(Guid lineId, long updatedBy)
    {
        EnsureDraft();
        if (updatedBy <= 0 || _lines.All(line => line.Id != lineId))
        {
            throw new DomainException("A matching invoice line and updater are required.");
        }

        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada ortak hesap motorundan gelen satır toplamlarını fatura başlığında birebir saklıyorum.
    public void ApplyTotals(
        decimal subtotalExcludingVat,
        decimal subtotalIncludingVat,
        decimal lineDiscountTotalExcludingVat,
        decimal lineDiscountTotalIncludingVat,
        decimal invoiceDiscountTotalExcludingVat,
        decimal invoiceDiscountTotalIncludingVat,
        decimal totalDiscountExcludingVat,
        decimal totalDiscountIncludingVat,
        decimal netAmountExcludingVat,
        decimal vatTotal,
        decimal grandTotalIncludingVat)
    {
        EnsureDraft();
        SubtotalExcludingVat = subtotalExcludingVat;
        SubtotalIncludingVat = subtotalIncludingVat;
        LineDiscountTotalExcludingVat = lineDiscountTotalExcludingVat;
        LineDiscountTotalIncludingVat = lineDiscountTotalIncludingVat;
        InvoiceDiscountTotalExcludingVat = invoiceDiscountTotalExcludingVat;
        InvoiceDiscountTotalIncludingVat = invoiceDiscountTotalIncludingVat;
        TotalDiscountExcludingVat = totalDiscountExcludingVat;
        TotalDiscountIncludingVat = totalDiscountIncludingVat;
        NetAmountExcludingVat = netAmountExcludingVat;
        VatTotal = vatTotal;
        GrandTotalIncludingVat = grandTotalIncludingVat;
        TotalAllocatedExpenseExcludingVat = 0m;
        TotalAllocatedExpenseIncludingVat = 0m;
        TotalFinalCostExcludingVat = netAmountExcludingVat;
        TotalFinalCostIncludingVat = grandTotalIncludingVat;
        PaidAmount = 0m;
        RemainingAmount = grandTotalIncludingVat;
    }

    // Burada tam tahsis edilmiş faturayı muhasebe etkileri hazırlandıktan sonra kesinleştiriyorum.
    public void MarkPosted(long postedBy, DateTime postedAt)
    {
        EnsureDraft();
        if (_lines.Count == 0 || postedBy <= 0 || postedAt == default)
        {
            throw new DomainException("A populated invoice, posting actor and time are required.");
        }

        Status = InvoiceStatus.Posted;
        PostedBy = postedBy;
        PostedAt = postedAt;
        MarkAsUpdated();
    }

    public void MarkCancelled(long cancelledBy, DateTime cancelledAt, string reason)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        if (Status != InvoiceStatus.Posted || cancelledBy <= 0 || cancelledAt == default ||
            string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Only a posted purchase invoice can be cancelled with audit information.");
        }

        Status = InvoiceStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = cancelledAt;
        CancellationReason = NormalizeOptional(reason, MaximumDescriptionLength);
        MarkAsUpdated();
    }

    // Burada post anındaki cari hesap kimlik, vergi, iletişim ve adres bilgilerini tarihsel snapshot olarak yeniliyorum.
    public void CaptureCurrentAccountSnapshot(CurrentAccount currentAccount)
    {
        EnsureDraft();
        if (currentAccount is null ||
            currentAccount.Id != CurrentAccountId ||
            !currentAccount.CanBeSupplier())
        {
            throw new DomainException("A matching active supplier current account is required.");
        }

        CurrentAccountNameSnapshot = currentAccount.Name;
        TaxNumberSnapshot = currentAccount.TaxNumber;
        TaxOfficeSnapshot = currentAccount.TaxOffice;
        PhoneNumberSnapshot = currentAccount.PhoneNumber;
        EmailSnapshot = currentAccount.Email;
        AddressSnapshot = BuildAddressSnapshot(currentAccount);
    }

    // Burada normal değişikliklerin yalnız taslak belgede yapılmasını koruyorum.
    public void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("Only draft purchase invoices can be changed.");
        }
    }

    // Burada gösterim bakiyesini geçerli cari hareket tahsislerinden gelen toplamla eşitliyorum.
    public void SynchronizePaymentBalance(decimal paidAmount)
    {
        if (paidAmount < 0m || paidAmount > GrandTotalIncludingVat)
        {
            throw new DomainException("Paid amount must be between zero and the invoice total.");
        }

        PaidAmount = paidAmount;
        RemainingAmount = GrandTotalIncludingVat - paidAmount;
        MarkAsUpdated();
    }

    public void ApplyExpenseTotals()
    {
        EnsureDraft();
        TotalAllocatedExpenseExcludingVat = decimal.Round(_lines.Sum(x => x.AllocatedExpenseExcludingVat), 2, MidpointRounding.AwayFromZero);
        TotalAllocatedExpenseIncludingVat = decimal.Round(_lines.Sum(x => x.AllocatedExpenseIncludingVat), 2, MidpointRounding.AwayFromZero);
        TotalFinalCostExcludingVat = NetAmountExcludingVat + TotalAllocatedExpenseExcludingVat;
        TotalFinalCostIncludingVat = GrandTotalIncludingVat + TotalAllocatedExpenseIncludingVat;
        MarkAsUpdated();
    }

    // Burada zorunlu metni temizleyip veritabanı uzunluğuna sığdırıyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    // Burada isteğe bağlı açıklamayı boş veya güvenli uzunlukta saklıyorum.
    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"Description cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    // Burada fatura tarihinin boş olmamasını doğruluyorum.
    private static DateTime EnsureDate(DateTime value, string fieldName)
    {
        return value == default
            ? throw new DomainException($"{fieldName} is required.")
            : value;
    }

    // Burada para birimi kodunu üç harfli kanonik değere dönüştürüyorum.
    private static string NormalizeCurrency(string value)
    {
        var normalized = NormalizeRequired(value, 3, "Currency code").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency code must contain three ASCII letters.");
        }

        return normalized;
    }

    // Burada alış faturasını onaylanan tek para birimi olan TRY ve sabit birim kurla sınırlandırıyorum.
    private void SetCurrency(string currencyCode, decimal exchangeRate)
    {
        var normalizedCurrencyCode = NormalizeCurrency(currencyCode);
        if (normalizedCurrencyCode != "TRY" || exchangeRate != 1m)
        {
            throw new DomainException(
                "Purchase accounting currently supports only TRY with exchange rate 1.");
        }

        CurrencyCode = normalizedCurrencyCode;
        ExchangeRate = exchangeRate;
    }

    // Burada tek cari hesap adres alanlarını faturada saklanacak değişmez bir metne birleştiriyorum.
    private static string? BuildAddressSnapshot(CurrentAccount currentAccount)
    {
        var parts = new[]
        {
            currentAccount.AddressLine,
            currentAccount.Neighborhood,
            currentAccount.District,
            currentAccount.City,
            currentAccount.Country,
            currentAccount.PostalCode
        }.Where(part => !string.IsNullOrWhiteSpace(part));
        var snapshot = string.Join(", ", parts);
        return snapshot.Length == 0 ? null : snapshot;
    }

    // Burada opsiyonel fatura indiriminin ham tanım alanlarını birlikte ve tutarlı saklıyorum.
    private void SetInvoiceDiscount(
        DiscountType? type,
        decimal? value,
        DiscountTaxBasis? taxBasis)
    {
        if (!type.HasValue && !value.HasValue && !taxBasis.HasValue)
        {
            InvoiceDiscountType = null;
            InvoiceDiscountValue = null;
            InvoiceDiscountTaxBasis = null;
            return;
        }

        if (!type.HasValue || !value.HasValue || !taxBasis.HasValue ||
            type is not DiscountType.Percentage and not DiscountType.FixedInvoiceTotal ||
            value.Value < 0m)
        {
            throw new DomainException("Invoice discount definition is incomplete or invalid.");
        }

        InvoiceDiscountType = type;
        InvoiceDiscountValue = value;
        InvoiceDiscountTaxBasis = taxBasis;
    }
}
