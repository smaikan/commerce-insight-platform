using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.SalesInvoices;

public sealed class SalesInvoice : AuditableEntity
{
    public const int MaximumInvoiceNumberLength = 100;
    public const int MaximumDescriptionLength = 500;
    public const int MaximumAddressSnapshotLength = 1500;
    private readonly List<SalesInvoiceLine> _lines = [];

    public Guid AccountingSalesOrderId { get; private set; }
    public AccountingSalesOrder AccountingSalesOrder { get; private set; } = null!;
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
    public decimal ShippingTotal { get; private set; }
    public ShippingPayer ShippingPayer { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal GrandTotalIncludingVat { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public decimal TotalCostOfGoodsSold { get; private set; }
    public decimal GrossProfitExcludingVat { get; private set; }
    public decimal GrossProfitMargin { get; private set; }
    public IReadOnlyCollection<SalesInvoiceLine> Lines => _lines.AsReadOnly();

    // Burada EF Core'un satış faturasını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private SalesInvoice()
    {
    }

    // Burada muhasebe satış siparişinin aynı satır ve toplamlarından isteğe bağlı iç satış faturasını oluşturuyorum.
    public SalesInvoice(
        AccountingSalesOrder order,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime? dueDate,
        string? description,
        long createdBy)
    {
        if (order is null ||
            order.Status == InvoiceStatus.Cancelled ||
            order.Items.Count == 0 ||
            createdBy <= 0)
        {
            throw new DomainException("A populated active sales order and creator are required.");
        }

        AccountingSalesOrderId = order.Id;
        AccountingSalesOrder = order;
        CurrentAccountId = order.CurrentAccountId;
        CurrentAccount = order.CurrentAccount;
        Status = InvoiceStatus.Draft;
        SetInvoiceHeader(invoiceNumber, invoiceDate, dueDate, description);
        CreatedBy = createdBy;
        order.AttachInvoice(this);
        CopyFromOrder(order);
    }

    // Burada yalnız taslak faturanın kendi belge numarası, tarihi ve açıklamasını güncelliyorum.
    public void UpdateHeader(
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime? dueDate,
        string? description,
        long updatedBy)
    {
        EnsureDraft();
        if (updatedBy <= 0)
        {
            throw new DomainException("A valid updater is required.");
        }

        SetInvoiceHeader(invoiceNumber, invoiceDate, dueDate, description);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada bağlı taslak faturanın satır, snapshot, toplam ve kârlılık değerlerini siparişle eşitliyorum.
    public void SyncFromOrder(AccountingSalesOrder order, long updatedBy)
    {
        EnsureDraft();
        EnsureMatchingOrder(order);
        if (updatedBy <= 0)
        {
            throw new DomainException("A valid updater is required.");
        }

        CopyFromOrder(order);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada bağlı siparişin güncel satır snapshot'larıyla taslak fatura satırlarını bütünüyle yeniliyorum.
    public void ReplaceLinesFromOrder(AccountingSalesOrder order, long updatedBy)
    {
        EnsureDraft();
        EnsureMatchingOrder(order);
        if (updatedBy <= 0)
        {
            throw new DomainException("A valid updater is required.");
        }

        ReplaceLinesCore(order);
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    // Burada post edilen siparişin son snapshot, FIFO maliyet ve toplamlarını alıp faturayı aynı transaction için kesinleştiriyorum.
    public void MarkPosted(long postedBy, DateTime postedAt)
    {
        EnsureDraft();
        if (AccountingSalesOrder.Status != InvoiceStatus.Posted ||
            postedBy <= 0 ||
            postedAt == default)
        {
            throw new DomainException("A posted sales order, posting actor and time are required.");
        }

        CopyFromOrder(AccountingSalesOrder);
        if (_lines.Count == 0 ||
            TotalCostOfGoodsSold != AccountingSalesOrder.TotalCostOfGoodsSold ||
            GrandTotalIncludingVat != AccountingSalesOrder.GrandTotalIncludingVat)
        {
            throw new DomainException("Sales invoice totals must exactly match the posted sales order.");
        }

        Status = InvoiceStatus.Posted;
        PostedBy = postedBy;
        PostedAt = postedAt;
        MarkAsUpdated();
    }

    public void MarkCancelledFromOrder(long cancelledBy, DateTime cancelledAt, string reason)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        if (AccountingSalesOrder.Status != InvoiceStatus.Cancelled ||
            cancelledBy <= 0 || cancelledAt == default || string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Sales invoice cancellation is owned by its cancelled accounting sales order.");
        }

        Status = InvoiceStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = cancelledAt;
        CancellationReason = NormalizeOptional(reason, MaximumDescriptionLength, "Cancellation reason");
        MarkAsUpdated();
    }

    // Burada normal değişikliklerin yalnız taslak satış faturasında yapılmasını koruyorum.
    public void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("Only draft sales invoices can be changed.");
        }
    }

    // Burada fatura görünümünü bağlı siparişin tek cari alacak tahsis bakiyesiyle eşitliyorum.
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

    // Burada fatura belge numarası, tarihi ve açıklamasını tek doğrulama noktasından uyguluyorum.
    private void SetInvoiceHeader(
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime? dueDate,
        string? description)
    {
        if (invoiceDate == default)
        {
            throw new DomainException("Invoice date is required.");
        }

        InvoiceNumber = NormalizeRequired(
            invoiceNumber,
            MaximumInvoiceNumberLength,
            "Invoice number");
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        Description = NormalizeOptional(
            description,
            MaximumDescriptionLength,
            "Description");
    }

    // Burada bağlı siparişin değişmez taraf snapshot'ı ile bütün finansal sonuçlarını faturaya kopyalıyorum.
    private void CopyFromOrder(AccountingSalesOrder order)
    {
        EnsureMatchingOrder(order);
        CurrentAccountId = order.CurrentAccountId;
        CurrentAccount = order.CurrentAccount;
        CurrentAccountNameSnapshot = order.CurrentAccountNameSnapshot;
        TaxNumberSnapshot = order.TaxNumberSnapshot;
        TaxOfficeSnapshot = order.TaxOfficeSnapshot;
        PhoneNumberSnapshot = order.PhoneNumberSnapshot;
        EmailSnapshot = order.EmailSnapshot;
        AddressSnapshot = order.AddressSnapshot;
        CurrencyCode = order.CurrencyCode;
        ExchangeRate = order.ExchangeRate;
        InvoiceDiscountType = order.InvoiceDiscountType;
        InvoiceDiscountValue = order.InvoiceDiscountValue;
        InvoiceDiscountTaxBasis = order.InvoiceDiscountTaxBasis;
        SubtotalExcludingVat = order.SubtotalExcludingVat;
        SubtotalIncludingVat = order.SubtotalIncludingVat;
        LineDiscountTotalExcludingVat = order.LineDiscountTotalExcludingVat;
        LineDiscountTotalIncludingVat = order.LineDiscountTotalIncludingVat;
        InvoiceDiscountTotalExcludingVat = order.InvoiceDiscountTotalExcludingVat;
        InvoiceDiscountTotalIncludingVat = order.InvoiceDiscountTotalIncludingVat;
        TotalDiscountExcludingVat = order.TotalDiscountExcludingVat;
        TotalDiscountIncludingVat = order.TotalDiscountIncludingVat;
        NetAmountExcludingVat = order.NetAmountExcludingVat;
        ShippingTotal = order.ShippingTotal;
        ShippingPayer = order.ShippingPayer;
        VatTotal = order.VatTotal;
        GrandTotalIncludingVat = order.GrandTotalIncludingVat;
        PaidAmount = order.PaidAmount;
        RemainingAmount = order.RemainingAmount;
        TotalCostOfGoodsSold = order.TotalCostOfGoodsSold;
        GrossProfitExcludingVat = order.GrossProfitExcludingVat;
        GrossProfitMargin = order.GrossProfitMargin;
        ReplaceLinesCore(order);
    }

    // Burada sipariş bağlantısı, cari hesap ve yaşam döngüsünün fatura senkronuna uygunluğunu doğruluyorum.
    private void EnsureMatchingOrder(AccountingSalesOrder order)
    {
        if (order is null ||
            order.Id != AccountingSalesOrderId ||
            order.Status == InvoiceStatus.Cancelled)
        {
            throw new DomainException("A matching non-cancelled accounting sales order is required.");
        }
    }

    // Burada sipariş satırlarını aynı sıra ve gerçekleşmiş maliyet değerleriyle yeni fatura snapshot'larına dönüştürüyorum.
    private void ReplaceLinesCore(AccountingSalesOrder order)
    {
        if (order.Items.Count == 0)
        {
            throw new DomainException("A sales invoice requires at least one sales order item.");
        }

        _lines.Clear();
        foreach (var item in order.Items.OrderBy(item => item.LineNumber))
        {
            _lines.Add(new SalesInvoiceLine(this, item));
        }
    }

    // Burada zorunlu fatura metnini temizleyip güvenli uzunlukta saklıyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} cannot be empty.");
    }

    // Burada isteğe bağlı fatura metnini boş veya güvenli uzunlukta saklıyorum.
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
}
