using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.PurchaseInvoices;

public sealed class PurchaseInvoiceLine : BaseEntity
{
    private readonly List<PurchaseInvoiceStockAllocation> _allocations = [];

    public Guid PurchaseInvoiceId { get; private set; }
    public PurchaseInvoice PurchaseInvoice { get; private set; } = null!;
    public int LineNumber { get; private set; }
    public long ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = null!;
    public string VariantNameSnapshot { get; private set; } = null!;
    public string SkuSnapshot { get; private set; } = null!;
    public string? BarcodeSnapshot { get; private set; }
    public decimal PurchaseQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = null!;
    public decimal UnitsPerPurchaseUnit { get; private set; }
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
    public decimal AllocatedExpenseExcludingVat { get; private set; }
    public decimal AllocatedExpenseIncludingVat { get; private set; }
    public decimal FinalTotalCostExcludingVat { get; private set; }
    public decimal FinalTotalCostIncludingVat { get; private set; }
    public decimal FinalUnitCostExcludingVat { get; private set; }
    public decimal FinalUnitCostIncludingVat { get; private set; }
    public IReadOnlyCollection<PurchaseInvoiceStockAllocation> Allocations => _allocations.AsReadOnly();

    // Burada EF Core'un fatura satırını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private PurchaseInvoiceLine()
    {
    }

    // Burada ürün snapshot'ı, ham girdi ve sunucu hesaplarını tek değişmez fatura satırında birleştiriyorum.
    public PurchaseInvoiceLine(
        PurchaseInvoice invoice,
        int lineNumber,
        long productId,
        Guid productVariantId,
        string productName,
        string variantName,
        string sku,
        string? barcode,
        decimal purchaseQuantity,
        string unitOfMeasure,
        decimal unitsPerPurchaseUnit,
        int stockQuantity,
        PriceEntryMode priceEntryMode,
        decimal enteredUnitPrice,
        decimal vatRate,
        DiscountType? lineDiscountType,
        decimal? lineDiscountValue,
        DiscountTaxBasis? lineDiscountTaxBasis,
        DiscountUnitBasis? lineDiscountUnitBasis,
        bool isInvoiceDiscountEligible)
    {
        if (invoice is null || productId <= 0 || productVariantId == Guid.Empty || lineNumber <= 0)
        {
            throw new DomainException("Invoice, product, variant and line number are required.");
        }

        if (purchaseQuantity <= 0m || unitsPerPurchaseUnit <= 0m || stockQuantity <= 0)
        {
            throw new DomainException("Purchase and stock quantities must be greater than zero.");
        }

        PurchaseInvoiceId = invoice.Id;
        PurchaseInvoice = invoice;
        LineNumber = lineNumber;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductNameSnapshot = Normalize(productName, 250, "Product name");
        VariantNameSnapshot = Normalize(variantName, 250, "Variant name");
        SkuSnapshot = Normalize(sku, 100, "SKU");
        BarcodeSnapshot = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        PurchaseQuantity = purchaseQuantity;
        UnitOfMeasure = Normalize(unitOfMeasure, 50, "Unit of measure");
        UnitsPerPurchaseUnit = unitsPerPurchaseUnit;
        StockQuantity = stockQuantity;
        PriceEntryMode = priceEntryMode;
        EnteredUnitPrice = enteredUnitPrice;
        VatRate = vatRate;
        LineDiscountType = lineDiscountType;
        LineDiscountValue = lineDiscountValue;
        LineDiscountTaxBasis = lineDiscountTaxBasis;
        LineDiscountUnitBasis = lineDiscountUnitBasis;
        IsInvoiceDiscountEligible = isInvoiceDiscountEligible;
    }

    // Burada merkezi hesap motorunun sonucunu satır maliyet alanlarına yansıtıyorum.
    public void ApplyCalculation(
        decimal unitPriceExcludingVat,
        decimal unitPriceIncludingVat,
        decimal grossAmountExcludingVat,
        decimal grossAmountIncludingVat,
        decimal lineDiscountAmountExcludingVat,
        decimal lineDiscountAmountIncludingVat,
        decimal invoiceDiscountShareExcludingVat,
        decimal invoiceDiscountShareIncludingVat,
        decimal totalDiscountAmountExcludingVat,
        decimal totalDiscountAmountIncludingVat,
        decimal netAmountExcludingVat,
        decimal vatAmount,
        decimal totalAmountIncludingVat)
    {
        PurchaseInvoice.EnsureDraft();
        UnitPriceExcludingVat = unitPriceExcludingVat;
        UnitPriceIncludingVat = unitPriceIncludingVat;
        GrossAmountExcludingVat = grossAmountExcludingVat;
        GrossAmountIncludingVat = grossAmountIncludingVat;
        LineDiscountAmountExcludingVat = lineDiscountAmountExcludingVat;
        LineDiscountAmountIncludingVat = lineDiscountAmountIncludingVat;
        InvoiceDiscountShareExcludingVat = invoiceDiscountShareExcludingVat;
        InvoiceDiscountShareIncludingVat = invoiceDiscountShareIncludingVat;
        TotalDiscountAmountExcludingVat = totalDiscountAmountExcludingVat;
        TotalDiscountAmountIncludingVat = totalDiscountAmountIncludingVat;
        NetAmountExcludingVat = netAmountExcludingVat;
        VatAmount = vatAmount;
        TotalAmountIncludingVat = totalAmountIncludingVat;
        AllocatedExpenseExcludingVat = 0m;
        AllocatedExpenseIncludingVat = 0m;
        FinalTotalCostExcludingVat = netAmountExcludingVat;
        FinalTotalCostIncludingVat = totalAmountIncludingVat;
        FinalUnitCostExcludingVat = decimal.Round(
            netAmountExcludingVat / StockQuantity,
            4,
            MidpointRounding.AwayFromZero);
        FinalUnitCostIncludingVat = decimal.Round(
            totalAmountIncludingVat / StockQuantity,
            4,
            MidpointRounding.AwayFromZero);
    }

    // Burada mevcut Purchase stok hareketinden onaylanan miktarı satıra bağlıyorum.
    // Burada ürün kimliği ve ilk snapshot alanlarına dokunmadan taslak satırın ticari değerlerini ve stok karşılığını güncelliyorum.
    public void UpdateCommercialTerms(
        decimal purchaseQuantity,
        string unitOfMeasure,
        decimal unitsPerPurchaseUnit,
        int stockQuantity,
        PriceEntryMode priceEntryMode,
        decimal enteredUnitPrice,
        decimal vatRate,
        DiscountType? lineDiscountType,
        decimal? lineDiscountValue,
        DiscountTaxBasis? lineDiscountTaxBasis,
        DiscountUnitBasis? lineDiscountUnitBasis,
        bool isInvoiceDiscountEligible)
    {
        PurchaseInvoice.EnsureDraft();
        if (purchaseQuantity <= 0m || unitsPerPurchaseUnit <= 0m || stockQuantity <= 0)
        {
            throw new DomainException("Purchase and stock quantities must be greater than zero.");
        }

        if (_allocations.Sum(item => item.AllocatedQuantity) > stockQuantity)
        {
            throw new DomainException(
                "Existing stock movement allocations cannot exceed the updated stock quantity.");
        }

        PurchaseQuantity = purchaseQuantity;
        UnitOfMeasure = Normalize(unitOfMeasure, 50, "Unit of measure");
        UnitsPerPurchaseUnit = unitsPerPurchaseUnit;
        StockQuantity = stockQuantity;
        PriceEntryMode = priceEntryMode;
        EnteredUnitPrice = enteredUnitPrice;
        VatRate = vatRate;
        LineDiscountType = lineDiscountType;
        LineDiscountValue = lineDiscountValue;
        LineDiscountTaxBasis = lineDiscountTaxBasis;
        LineDiscountUnitBasis = lineDiscountUnitBasis;
        IsInvoiceDiscountEligible = isInvoiceDiscountEligible;
    }

    // Burada mevcut Purchase stok hareketinden onaylanan miktarı satıra bağlıyorum.
    public PurchaseInvoiceStockAllocation AddAllocation(Guid stockMovementId, int allocatedQuantity)
    {
        PurchaseInvoice.EnsureDraft();
        if (stockMovementId == Guid.Empty || allocatedQuantity <= 0)
        {
            throw new DomainException("Stock movement and positive allocation quantity are required.");
        }

        if (_allocations.Any(item => item.StockMovementId == stockMovementId))
        {
            throw new DomainException("The stock movement is already allocated to this invoice line.");
        }

        if (_allocations.Sum(item => item.AllocatedQuantity) + allocatedQuantity > StockQuantity)
        {
            throw new DomainException("Invoice line allocation cannot exceed stock quantity.");
        }

        var allocation = new PurchaseInvoiceStockAllocation(this, stockMovementId, allocatedQuantity);
        _allocations.Add(allocation);
        return allocation;
    }

    // Burada taslak satırın mevcut allocation kayıtlarını yeni doğrulanmış dağıtım için temizliyorum.
    public void ClearAllocations()
    {
        PurchaseInvoice.EnsureDraft();
        _allocations.Clear();
    }

    // Burada satırın post için tam miktarda tahsis edilip edilmediğini bildiriyorum.
    public bool IsFullyAllocated()
    {
        return _allocations.Sum(item => item.AllocatedQuantity) == StockQuantity;
    }

    public void ApplyAllocatedExpense(decimal excludingVat, decimal includingVat)
    {
        PurchaseInvoice.EnsureDraft();
        if (excludingVat < 0m || includingVat < excludingVat)
            throw new DomainException("Allocated expense amounts are invalid.");

        AllocatedExpenseExcludingVat = decimal.Round(excludingVat, 2, MidpointRounding.AwayFromZero);
        AllocatedExpenseIncludingVat = decimal.Round(includingVat, 2, MidpointRounding.AwayFromZero);
        FinalTotalCostExcludingVat = NetAmountExcludingVat + AllocatedExpenseExcludingVat;
        FinalTotalCostIncludingVat = TotalAmountIncludingVat + AllocatedExpenseIncludingVat;
        FinalUnitCostExcludingVat = decimal.Round(FinalTotalCostExcludingVat / StockQuantity, 4, MidpointRounding.AwayFromZero);
        FinalUnitCostIncludingVat = decimal.Round(FinalTotalCostIncludingVat / StockQuantity, 4, MidpointRounding.AwayFromZero);
    }

    // Burada snapshot metinlerini zorunlu ve güvenli uzunlukta saklıyorum.
    private static string Normalize(string value, int maximumLength, string fieldName)
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
}

public sealed class PurchaseInvoiceStockAllocation : BaseEntity
{
    public Guid PurchaseInvoiceLineId { get; private set; }
    public PurchaseInvoiceLine PurchaseInvoiceLine { get; private set; } = null!;
    public Guid StockMovementId { get; private set; }
    public int AllocatedQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un allocation kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private PurchaseInvoiceStockAllocation()
    {
    }

    // Burada fatura satırı ile mevcut stok hareketi arasındaki maliyet miktarını bağlıyorum.
    internal PurchaseInvoiceStockAllocation(
        PurchaseInvoiceLine line,
        Guid stockMovementId,
        int allocatedQuantity)
    {
        if (line is null || stockMovementId == Guid.Empty || allocatedQuantity <= 0)
        {
            throw new DomainException("Invoice line, stock movement and allocation quantity are required.");
        }

        PurchaseInvoiceLineId = line.Id;
        PurchaseInvoiceLine = line;
        StockMovementId = stockMovementId;
        AllocatedQuantity = allocatedQuantity;
        CreatedAt = DateTime.UtcNow;
    }
}
