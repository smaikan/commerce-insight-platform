using ECommerce.Domain.Accounting.Common;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.SalesOrders;

public sealed class AccountingSalesOrder : AuditableEntity
{
    public const int MaximumOrderNumberLength = 100;
    public const int MaximumIdempotencyKeyLength = 100;
    public const int MaximumDescriptionLength = 500;
    public const int MaximumAddressSnapshotLength = 1500;
    private readonly List<AccountingSalesOrderItem> _items = [];

    public string IdempotencyKey { get; private set; } = null!;
    public string OrderNumber { get; private set; } = null!;
    public Guid CurrentAccountId { get; private set; }
    public CurrentAccount CurrentAccount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal ExchangeRate { get; private set; }
    public DiscountType? InvoiceDiscountType { get; private set; }
    public decimal? InvoiceDiscountValue { get; private set; }
    public DiscountTaxBasis? InvoiceDiscountTaxBasis { get; private set; }
    public decimal ShippingTotal { get; private set; }
    public ShippingPayer ShippingPayer { get; private set; }
    public string? Description { get; private set; }
    public string CurrentAccountNameSnapshot { get; private set; } = null!;
    public string? TaxNumberSnapshot { get; private set; }
    public string? TaxOfficeSnapshot { get; private set; }
    public string? PhoneNumberSnapshot { get; private set; }
    public string? EmailSnapshot { get; private set; }
    public string? AddressSnapshot { get; private set; }
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
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public decimal TotalCostOfGoodsSold { get; private set; }
    public decimal GrossProfitExcludingVat { get; private set; }
    public decimal GrossProfitMargin { get; private set; }
    public IReadOnlyCollection<AccountingSalesOrderItem> Items => _items.AsReadOnly();
    public SalesInvoice? SalesInvoice { get; private set; }

    // Burada EF Core'un muhasebe satış siparişini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private AccountingSalesOrder()
    {
    }

    // Burada kullanıcı, sepet veya teslimat adresi gerektirmeyen taslak muhasebe satış siparişini oluşturuyorum.
    public AccountingSalesOrder(
        CurrentAccount currentAccount,
        string idempotencyKey,
        string orderNumber,
        DateTime orderDate,
        DateTime? dueDate,
        string currencyCode,
        decimal exchangeRate,
        DiscountType? invoiceDiscountType,
        decimal? invoiceDiscountValue,
        DiscountTaxBasis? invoiceDiscountTaxBasis,
        decimal shippingTotal,
        ShippingPayer shippingPayer,
        string? description,
        long createdBy)
    {
        if (currentAccount is null || !currentAccount.CanBeCustomer() || createdBy <= 0)
        {
            throw new DomainException("An active customer current account and creator are required.");
        }

        IdempotencyKey = NormalizeRequired(
            idempotencyKey,
            MaximumIdempotencyKeyLength,
            "Idempotency key");
        CurrentAccountId = currentAccount.Id;
        CurrentAccount = currentAccount;
        Status = InvoiceStatus.Draft;
        SetHeaderData(
            orderNumber,
            orderDate,
            dueDate,
            currencyCode,
            exchangeRate,
            invoiceDiscountType,
            invoiceDiscountValue,
            invoiceDiscountTaxBasis,
            shippingTotal,
            shippingPayer,
            description);
        CaptureCurrentAccountSnapshot(currentAccount);
        CreatedBy = createdBy;
    }

    // Burada yalnız taslak siparişin cari hesap ve ticari başlık bilgilerini güncelliyorum.
    public void UpdateHeader(
        CurrentAccount currentAccount,
        string orderNumber,
        DateTime orderDate,
        DateTime? dueDate,
        string currencyCode,
        decimal exchangeRate,
        DiscountType? invoiceDiscountType,
        decimal? invoiceDiscountValue,
        DiscountTaxBasis? invoiceDiscountTaxBasis,
        decimal shippingTotal,
        ShippingPayer shippingPayer,
        string? description,
        long updatedBy)
    {
        EnsureDraft();
        if (currentAccount is null || !currentAccount.CanBeCustomer() || updatedBy <= 0)
        {
            throw new DomainException("An active customer current account and updater are required.");
        }

        CurrentAccountId = currentAccount.Id;
        CurrentAccount = currentAccount;
        SetHeaderData(
            orderNumber,
            orderDate,
            dueDate,
            currencyCode,
            exchangeRate,
            invoiceDiscountType,
            invoiceDiscountValue,
            invoiceDiscountTaxBasis,
            shippingTotal,
            shippingPayer,
            description);
        CaptureCurrentAccountSnapshot(currentAccount);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada hesaplanmış yeni satış satırını taslak siparişe ekliyorum.
    public void AddItem(AccountingSalesOrderItem item, long updatedBy)
    {
        EnsureDraft();
        if (item is null || item.AccountingSalesOrderId != Id || updatedBy <= 0)
        {
            throw new DomainException("A matching sales order item and updater are required.");
        }

        if (_items.Any(existing => existing.LineNumber == item.LineNumber))
        {
            throw new DomainException("Sales order line numbers must be unique.");
        }

        _items.Add(item);
        UpdatedBy = updatedBy;
        ResetCostAndProfitTotals();
        MarkAsUpdated();
    }

    // Burada taslak sipariş satırını yeni hesaplanmış satırla değiştirip eski kaydı geri döndürüyorum.
    public AccountingSalesOrderItem ReplaceItem(
        Guid itemId,
        AccountingSalesOrderItem replacement,
        long updatedBy)
    {
        EnsureDraft();
        if (replacement is null || replacement.AccountingSalesOrderId != Id || updatedBy <= 0)
        {
            throw new DomainException("A matching replacement item and updater are required.");
        }

        var index = _items.FindIndex(item => item.Id == itemId);
        if (index < 0)
        {
            throw new DomainException("Sales order item was not found.");
        }

        if (_items.Any(item => item.Id != itemId && item.LineNumber == replacement.LineNumber))
        {
            throw new DomainException("Sales order line numbers must be unique.");
        }

        var removed = _items[index];
        _items[index] = replacement;
        UpdatedBy = updatedBy;
        ResetCostAndProfitTotals();
        MarkAsUpdated();
        return removed;
    }

    // Burada seçili satırı taslak siparişten kaldırıp kalıcılık katmanının izleyebilmesi için geri döndürüyorum.
    public AccountingSalesOrderItem RemoveItem(Guid itemId, long updatedBy)
    {
        EnsureDraft();
        if (updatedBy <= 0)
        {
            throw new DomainException("A valid updater is required.");
        }

        var item = _items.SingleOrDefault(existing => existing.Id == itemId)
            ?? throw new DomainException("Sales order item was not found.");
        _items.Remove(item);
        UpdatedBy = updatedBy;
        ResetCostAndProfitTotals();
        MarkAsUpdated();
        return item;
    }

    // Burada satır toplamlarını doğrulayıp yalnız müşteri ödemeli KDV'siz kargoyu nihai tutarlara ekliyorum.
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
        decimal lineGrandTotalIncludingVat)
    {
        EnsureDraft();
        if (_items.Count == 0)
        {
            throw new DomainException("A sales order must contain at least one item before totals are applied.");
        }

        EnsureTotalsMatchItems(
            subtotalExcludingVat,
            subtotalIncludingVat,
            lineDiscountTotalExcludingVat,
            lineDiscountTotalIncludingVat,
            invoiceDiscountTotalExcludingVat,
            invoiceDiscountTotalIncludingVat,
            totalDiscountExcludingVat,
            totalDiscountIncludingVat,
            netAmountExcludingVat,
            vatTotal,
            lineGrandTotalIncludingVat);

        SubtotalExcludingVat = Money(subtotalExcludingVat);
        SubtotalIncludingVat = Money(subtotalIncludingVat);
        LineDiscountTotalExcludingVat = Money(lineDiscountTotalExcludingVat);
        LineDiscountTotalIncludingVat = Money(lineDiscountTotalIncludingVat);
        InvoiceDiscountTotalExcludingVat = Money(invoiceDiscountTotalExcludingVat);
        InvoiceDiscountTotalIncludingVat = Money(invoiceDiscountTotalIncludingVat);
        TotalDiscountExcludingVat = Money(totalDiscountExcludingVat);
        TotalDiscountIncludingVat = Money(totalDiscountIncludingVat);
        var customerShipping =
            ShippingPayer ==
            global::ECommerce.Domain.Accounting.Common.Enums.ShippingPayer.Customer
            ? ShippingTotal
            : 0m;
        NetAmountExcludingVat = Money(netAmountExcludingVat + customerShipping);
        VatTotal = Money(vatTotal);
        GrandTotalIncludingVat = Money(lineGrandTotalIncludingVat + customerShipping);
        PaidAmount = 0m;
        RemainingAmount = GrandTotalIncludingVat;
        ResetCostAndProfitTotals();
    }

    // Burada gerçekleşen FIFO tüketimlerinden satır ve başlık maliyet ile kârlılık değerlerini üretiyorum.
    public void ApplyProfitability()
    {
        EnsureDraft();
        if (_items.Count == 0)
        {
            throw new DomainException("A sales order must contain at least one item.");
        }

        foreach (var item in _items.OrderBy(item => item.LineNumber))
        {
            item.ApplyProfitability();
        }

        TotalCostOfGoodsSold = Money(_items.Sum(item => item.CostOfGoodsSold));
        GrossProfitExcludingVat = Money(_items.Sum(item => item.GrossProfitExcludingVat));
        var productNetAmount = Money(_items.Sum(item => item.NetAmountExcludingVat));
        GrossProfitMargin = CalculateMargin(GrossProfitExcludingVat, productNetAmount);
    }

    // Burada post anındaki aktif müşteri kimlik, vergi, iletişim ve adres bilgilerini tarihsel snapshot olarak alıyorum.
    public void CaptureCurrentAccountSnapshot(CurrentAccount currentAccount)
    {
        EnsureDraft();
        if (currentAccount is null ||
            currentAccount.Id != CurrentAccountId ||
            !currentAccount.CanBeCustomer())
        {
            throw new DomainException("A matching active customer current account is required.");
        }

        CurrentAccountNameSnapshot = currentAccount.Name;
        TaxNumberSnapshot = currentAccount.TaxNumber;
        TaxOfficeSnapshot = currentAccount.TaxOffice;
        PhoneNumberSnapshot = currentAccount.PhoneNumber;
        EmailSnapshot = currentAccount.Email;
        AddressSnapshot = BuildAddressSnapshot(currentAccount);
    }

    // Burada isteğe bağlı faturayı siparişe yalnız bir kez ve aynı cari hesapla bağlıyorum.
    public void AttachInvoice(SalesInvoice invoice)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            throw new DomainException("A cancelled sales order cannot receive an invoice.");
        }

        if (invoice is null ||
            invoice.AccountingSalesOrderId != Id ||
            invoice.CurrentAccountId != CurrentAccountId)
        {
            throw new DomainException("A matching sales invoice is required.");
        }

        if (SalesInvoice is not null && SalesInvoice.Id != invoice.Id)
        {
            throw new DomainException("A sales order can have only one sales invoice.");
        }

        SalesInvoice = invoice;
    }

    // Burada tüm stok, FIFO ve kârlılık etkileri tamamlanan taslak siparişi kesinleştiriyorum.
    public void MarkPosted(long postedBy, DateTime postedAt)
    {
        EnsureDraft();
        if (_items.Count == 0 || postedBy <= 0 || postedAt == default)
        {
            throw new DomainException("A populated sales order, posting actor and time are required.");
        }

        if (_items.Any(item => !item.HasCompleteStockEffect() || !item.HasCompleteCostConsumption()))
        {
            throw new DomainException("Every sales order item must have complete stock-out and FIFO consumption.");
        }

        ApplyProfitability();
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
            throw new DomainException("Only a posted sales order can be cancelled with audit information.");
        }

        Status = InvoiceStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = cancelledAt;
        CancellationReason = NormalizeOptional(reason, MaximumDescriptionLength, "Cancellation reason");
        SalesInvoice?.MarkCancelledFromOrder(cancelledBy, cancelledAt, reason);
        MarkAsUpdated();
    }

    // Burada normal değişikliklerin yalnız taslak muhasebe satış siparişinde yapılmasını koruyorum.
    public void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("Only draft accounting sales orders can be changed.");
        }
    }

    // Burada sipariş ve opsiyonel faturanın tek cari alacak tahsisinden gelen gösterim bakiyesini eşitliyorum.
    public void SynchronizePaymentBalance(decimal paidAmount)
    {
        if (paidAmount < 0m || paidAmount > GrandTotalIncludingVat)
        {
            throw new DomainException("Paid amount must be between zero and the sales order total.");
        }

        PaidAmount = paidAmount;
        RemainingAmount = GrandTotalIncludingVat - paidAmount;
        SalesInvoice?.SynchronizePaymentBalance(paidAmount);
        MarkAsUpdated();
    }

    // Burada siparişin değiştirilebilir ticari başlık alanlarını tek doğrulama noktasından uyguluyorum.
    private void SetHeaderData(
        string orderNumber,
        DateTime orderDate,
        DateTime? dueDate,
        string currencyCode,
        decimal exchangeRate,
        DiscountType? invoiceDiscountType,
        decimal? invoiceDiscountValue,
        DiscountTaxBasis? invoiceDiscountTaxBasis,
        decimal shippingTotal,
        ShippingPayer shippingPayer,
        string? description)
    {
        if (orderDate == default || shippingTotal < 0m)
        {
            throw new DomainException("Order date and non-negative shipping total are required.");
        }

        var normalizedCurrencyCode = NormalizeCurrency(currencyCode);
        if (normalizedCurrencyCode != "TRY" || exchangeRate != 1m)
        {
            throw new DomainException("Accounting sales currently supports only TRY with exchange rate 1.");
        }

        var normalizedShippingTotal = Money(shippingTotal);
        if (!Enum.IsDefined(shippingPayer) ||
            (normalizedShippingTotal == 0m && shippingPayer != ShippingPayer.None) ||
            (normalizedShippingTotal > 0m &&
             shippingPayer is not ShippingPayer.Seller and not ShippingPayer.Customer))
        {
            throw new DomainException("Shipping payer must match the supplied shipping total.");
        }

        OrderNumber = NormalizeRequired(orderNumber, MaximumOrderNumberLength, "Order number");
        OrderDate = orderDate;
        DueDate = dueDate;
        CurrencyCode = normalizedCurrencyCode;
        ExchangeRate = exchangeRate;
        SetDocumentDiscount(
            invoiceDiscountType,
            invoiceDiscountValue,
            invoiceDiscountTaxBasis);
        ShippingTotal = normalizedShippingTotal;
        ShippingPayer = shippingPayer;
        Description = NormalizeOptional(description, MaximumDescriptionLength, "Description");
    }

    // Burada opsiyonel belge indiriminin ham tanım alanlarını birlikte ve tutarlı saklıyorum.
    private void SetDocumentDiscount(
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

        if (!type.HasValue ||
            !value.HasValue ||
            !taxBasis.HasValue ||
            type is not DiscountType.Percentage and not DiscountType.FixedInvoiceTotal ||
            value.Value < 0m)
        {
            throw new DomainException("Document discount definition is incomplete or invalid.");
        }

        InvoiceDiscountType = type;
        InvoiceDiscountValue = value;
        InvoiceDiscountTaxBasis = taxBasis;
    }

    // Burada hesap motorundan gelen başlık değerlerinin satır toplamlarıyla tam eşleşmesini doğruluyorum.
    private void EnsureTotalsMatchItems(
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
        decimal lineGrandTotalIncludingVat)
    {
        var expected = new[]
        {
            Money(_items.Sum(item => item.GrossAmountExcludingVat)),
            Money(_items.Sum(item => item.GrossAmountIncludingVat)),
            Money(_items.Sum(item => item.LineDiscountAmountExcludingVat)),
            Money(_items.Sum(item => item.LineDiscountAmountIncludingVat)),
            Money(_items.Sum(item => item.InvoiceDiscountShareExcludingVat)),
            Money(_items.Sum(item => item.InvoiceDiscountShareIncludingVat)),
            Money(_items.Sum(item => item.TotalDiscountAmountExcludingVat)),
            Money(_items.Sum(item => item.TotalDiscountAmountIncludingVat)),
            Money(_items.Sum(item => item.NetAmountExcludingVat)),
            Money(_items.Sum(item => item.VatAmount)),
            Money(_items.Sum(item => item.TotalAmountIncludingVat))
        };
        var supplied = new[]
        {
            Money(subtotalExcludingVat),
            Money(subtotalIncludingVat),
            Money(lineDiscountTotalExcludingVat),
            Money(lineDiscountTotalIncludingVat),
            Money(invoiceDiscountTotalExcludingVat),
            Money(invoiceDiscountTotalIncludingVat),
            Money(totalDiscountExcludingVat),
            Money(totalDiscountIncludingVat),
            Money(netAmountExcludingVat),
            Money(vatTotal),
            Money(lineGrandTotalIncludingVat)
        };

        if (!expected.SequenceEqual(supplied))
        {
            throw new DomainException("Sales order header totals must exactly match item totals.");
        }
    }

    // Burada cari hesabın tek adres alanlarını değişmez sipariş snapshot metnine birleştiriyorum.
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
        if (snapshot.Length > MaximumAddressSnapshotLength)
        {
            throw new DomainException(
                $"Address snapshot cannot exceed {MaximumAddressSnapshotLength} characters.");
        }

        return snapshot.Length == 0 ? null : snapshot;
    }

    // Burada zorunlu metni temizleyip güvenli uzunlukta saklıyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} cannot be empty.");
    }

    // Burada isteğe bağlı metni boş veya güvenli uzunlukta saklıyorum.
    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string fieldName)
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

    // Burada parasal değerleri Accounting para hassasiyetine yuvarlıyorum.
    private static decimal Money(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.InvoiceTotalScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada KDV hariç gelir sıfırken bölme yapmadan brüt kâr marjını hesaplıyorum.
    private static decimal CalculateMargin(decimal grossProfit, decimal netAmountExcludingVat)
    {
        if (netAmountExcludingVat == 0m)
        {
            return 0m;
        }

        return decimal.Round(
            grossProfit / netAmountExcludingVat * 100m,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada satırlar yeniden hesaplandığında kesinleşmemiş maliyet ve kâr değerlerini temizliyorum.
    private void ResetCostAndProfitTotals()
    {
        TotalCostOfGoodsSold = 0m;
        GrossProfitExcludingVat = 0m;
        GrossProfitMargin = 0m;
    }
}
