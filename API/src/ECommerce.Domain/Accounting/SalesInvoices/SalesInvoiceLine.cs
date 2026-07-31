using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.SalesInvoices;

public sealed class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; private set; }
    public SalesInvoice SalesInvoice { get; private set; } = null!;
    public Guid AccountingSalesOrderItemId { get; private set; }
    public AccountingSalesOrderItem AccountingSalesOrderItem { get; private set; } = null!;
    public int LineNumber { get; private set; }
    public long ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = null!;
    public string VariantNameSnapshot { get; private set; } = null!;
    public string SkuSnapshot { get; private set; } = null!;
    public string? BarcodeSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = null!;
    public decimal UnitsPerSaleUnit { get; private set; }
    public int StockQuantity { get; private set; }
    public PriceEntryMode PriceEntryMode { get; private set; }
    public decimal EnteredUnitPrice { get; private set; }
    public decimal UnitPriceExcludingVat { get; private set; }
    public decimal UnitPriceIncludingVat { get; private set; }
    public decimal VatRate { get; private set; }
    public DiscountType? LineDiscountType { get; private set; }
    public decimal? LineDiscountValue { get; private set; }
    public DiscountTaxBasis? LineDiscountTaxBasis { get; private set; }
    public DiscountUnitBasis? LineDiscountUnitBasis { get; private set; }
    public bool IsInvoiceDiscountEligible { get; private set; }
    public decimal GrossAmountExcludingVat { get; private set; }
    public decimal GrossAmountIncludingVat { get; private set; }
    public decimal LineDiscountAmountExcludingVat { get; private set; }
    public decimal LineDiscountAmountIncludingVat { get; private set; }
    public decimal InvoiceDiscountShareExcludingVat { get; private set; }
    public decimal InvoiceDiscountShareIncludingVat { get; private set; }
    public decimal TotalDiscountAmountExcludingVat { get; private set; }
    public decimal TotalDiscountAmountIncludingVat { get; private set; }
    public decimal NetAmountExcludingVat { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalAmountIncludingVat { get; private set; }
    public decimal CostOfGoodsSold { get; private set; }
    public decimal GrossProfitExcludingVat { get; private set; }
    public decimal GrossProfitMargin { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un satış faturası satırını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private SalesInvoiceLine()
    {
    }

    // Burada sipariş satırının ürün, hesap ve gerçekleşmiş FIFO maliyet snapshot'ını faturaya kopyalıyorum.
    internal SalesInvoiceLine(
        SalesInvoice invoice,
        AccountingSalesOrderItem sourceItem)
    {
        if (invoice is null ||
            sourceItem is null ||
            invoice.Id == Guid.Empty ||
            sourceItem.Id == Guid.Empty ||
            sourceItem.AccountingSalesOrderId != invoice.AccountingSalesOrderId)
        {
            throw new DomainException("A matching sales invoice and sales order item are required.");
        }

        SalesInvoiceId = invoice.Id;
        SalesInvoice = invoice;
        AccountingSalesOrderItemId = sourceItem.Id;
        AccountingSalesOrderItem = sourceItem;
        LineNumber = sourceItem.LineNumber;
        ProductId = sourceItem.ProductId;
        ProductVariantId = sourceItem.ProductVariantId;
        ProductNameSnapshot = sourceItem.ProductNameSnapshot;
        VariantNameSnapshot = sourceItem.VariantNameSnapshot;
        SkuSnapshot = sourceItem.SkuSnapshot;
        BarcodeSnapshot = sourceItem.BarcodeSnapshot;
        Quantity = sourceItem.Quantity;
        UnitOfMeasure = sourceItem.UnitOfMeasure;
        UnitsPerSaleUnit = sourceItem.UnitsPerSaleUnit;
        StockQuantity = sourceItem.StockQuantity;
        PriceEntryMode = sourceItem.PriceEntryMode;
        EnteredUnitPrice = sourceItem.EnteredUnitPrice;
        UnitPriceExcludingVat = sourceItem.UnitPriceExcludingVat;
        UnitPriceIncludingVat = sourceItem.UnitPriceIncludingVat;
        VatRate = sourceItem.VatRate;
        LineDiscountType = sourceItem.LineDiscountType;
        LineDiscountValue = sourceItem.LineDiscountValue;
        LineDiscountTaxBasis = sourceItem.LineDiscountTaxBasis;
        LineDiscountUnitBasis = sourceItem.LineDiscountUnitBasis;
        IsInvoiceDiscountEligible = sourceItem.IsInvoiceDiscountEligible;
        GrossAmountExcludingVat = sourceItem.GrossAmountExcludingVat;
        GrossAmountIncludingVat = sourceItem.GrossAmountIncludingVat;
        LineDiscountAmountExcludingVat = sourceItem.LineDiscountAmountExcludingVat;
        LineDiscountAmountIncludingVat = sourceItem.LineDiscountAmountIncludingVat;
        InvoiceDiscountShareExcludingVat = sourceItem.InvoiceDiscountShareExcludingVat;
        InvoiceDiscountShareIncludingVat = sourceItem.InvoiceDiscountShareIncludingVat;
        TotalDiscountAmountExcludingVat = sourceItem.TotalDiscountAmountExcludingVat;
        TotalDiscountAmountIncludingVat = sourceItem.TotalDiscountAmountIncludingVat;
        NetAmountExcludingVat = sourceItem.NetAmountExcludingVat;
        VatAmount = sourceItem.VatAmount;
        TotalAmountIncludingVat = sourceItem.TotalAmountIncludingVat;
        CostOfGoodsSold = sourceItem.CostOfGoodsSold;
        GrossProfitExcludingVat = sourceItem.GrossProfitExcludingVat;
        GrossProfitMargin = sourceItem.GrossProfitMargin;
        CreatedAt = DateTime.UtcNow;
    }
}
