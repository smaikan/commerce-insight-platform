using ECommerce.Domain.Accounting.Common;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Accounting.SalesOrders;

public sealed class AccountingSalesOrderItem : BaseEntity
{
    public const int MaximumProductNameLength = 250;
    public const int MaximumVariantNameLength = 250;
    public const int MaximumSkuLength = 100;
    public const int MaximumBarcodeLength = 100;
    public const int MaximumUnitOfMeasureLength = 50;
    private readonly List<AccountingSalesOrderStockMovement> _stockMovements = [];
    private readonly List<CostLayerConsumption> _costLayerConsumptions = [];

    public Guid AccountingSalesOrderId { get; private set; }
    public AccountingSalesOrder AccountingSalesOrder { get; private set; } = null!;
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
    public IReadOnlyCollection<AccountingSalesOrderStockMovement> StockMovements =>
        _stockMovements.AsReadOnly();
    public IReadOnlyCollection<CostLayerConsumption> CostLayerConsumptions =>
        _costLayerConsumptions.AsReadOnly();

    // Burada EF Core'un muhasebe satış siparişi satırını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private AccountingSalesOrderItem()
    {
    }

    // Burada doğrudan Accounting girdisini güvenilir ürün snapshot'ı ve tam sayı stok miktarıyla satıra dönüştürüyorum.
    public AccountingSalesOrderItem(
        AccountingSalesOrder order,
        int lineNumber,
        long productId,
        Guid productVariantId,
        string productName,
        string variantName,
        string sku,
        string? barcode,
        decimal quantity,
        string unitOfMeasure,
        decimal unitsPerSaleUnit,
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
        if (order is null ||
            lineNumber <= 0 ||
            productId <= 0 ||
            productVariantId == Guid.Empty)
        {
            throw new DomainException("Order, line, product and variant values are required.");
        }

        order.EnsureDraft();
        EnsureQuantityContract(quantity, unitsPerSaleUnit, stockQuantity);
        if (!Enum.IsDefined(priceEntryMode) || enteredUnitPrice < 0m || vatRate is < 0m or > 100m)
        {
            throw new DomainException("Price mode, non-negative entered price and a valid VAT rate are required.");
        }

        AccountingSalesOrderId = order.Id;
        AccountingSalesOrder = order;
        LineNumber = lineNumber;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductNameSnapshot = NormalizeRequired(
            productName,
            MaximumProductNameLength,
            "Product name");
        VariantNameSnapshot = NormalizeRequired(
            variantName,
            MaximumVariantNameLength,
            "Variant name");
        SkuSnapshot = NormalizeRequired(sku, MaximumSkuLength, "SKU");
        BarcodeSnapshot = NormalizeOptional(barcode, MaximumBarcodeLength, "Barcode");
        Quantity = decimal.Round(
            quantity,
            AccountingPrecision.QuantityScale,
            AccountingPrecision.RoundingMode);
        UnitOfMeasure = NormalizeRequired(
            unitOfMeasure,
            MaximumUnitOfMeasureLength,
            "Unit of measure");
        UnitsPerSaleUnit = decimal.Round(
            unitsPerSaleUnit,
            AccountingPrecision.QuantityScale,
            AccountingPrecision.RoundingMode);
        StockQuantity = stockQuantity;
        PriceEntryMode = priceEntryMode;
        EnteredUnitPrice = decimal.Round(
            enteredUnitPrice,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
        VatRate = decimal.Round(
            vatRate,
            AccountingPrecision.PercentageScale,
            AccountingPrecision.RoundingMode);
        SetLineDiscount(
            lineDiscountType,
            lineDiscountValue,
            lineDiscountTaxBasis,
            lineDiscountUnitBasis);
        IsInvoiceDiscountEligible = isInvoiceDiscountEligible;
    }

    // Burada ilk ürün snapshot'ını değiştirmeden yalnız ticari alanları taşıyan yeni taslak satır üretiyorum.
    public AccountingSalesOrderItem CreateCommercialReplacement(
        AccountingSalesOrder order,
        int lineNumber,
        decimal quantity,
        string unitOfMeasure,
        decimal unitsPerSaleUnit,
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
        if (order is null || order.Id != AccountingSalesOrderId)
        {
            throw new DomainException("A matching draft sales order is required.");
        }

        return new AccountingSalesOrderItem(
            order,
            lineNumber,
            ProductId,
            ProductVariantId,
            ProductNameSnapshot,
            VariantNameSnapshot,
            SkuSnapshot,
            BarcodeSnapshot,
            quantity,
            unitOfMeasure,
            unitsPerSaleUnit,
            stockQuantity,
            priceEntryMode,
            enteredUnitPrice,
            vatRate,
            lineDiscountType,
            lineDiscountValue,
            lineDiscountTaxBasis,
            lineDiscountUnitBasis,
            isInvoiceDiscountEligible);
    }

    // Burada merkezi hesap motorunun sonucunu satırın satış ve indirim alanlarına doğrulayarak uyguluyorum.
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
        AccountingSalesOrder.EnsureDraft();
        if (_stockMovements.Count > 0 || _costLayerConsumptions.Count > 0)
        {
            throw new DomainException("A stock-affected sales order item cannot be recalculated.");
        }

        EnsureCalculatedAmounts(
            unitPriceExcludingVat,
            unitPriceIncludingVat,
            grossAmountExcludingVat,
            grossAmountIncludingVat,
            lineDiscountAmountExcludingVat,
            lineDiscountAmountIncludingVat,
            invoiceDiscountShareExcludingVat,
            invoiceDiscountShareIncludingVat,
            totalDiscountAmountExcludingVat,
            totalDiscountAmountIncludingVat,
            netAmountExcludingVat,
            vatAmount,
            totalAmountIncludingVat);

        UnitPriceExcludingVat = UnitPrice(unitPriceExcludingVat);
        UnitPriceIncludingVat = UnitPrice(unitPriceIncludingVat);
        GrossAmountExcludingVat = Money(grossAmountExcludingVat);
        GrossAmountIncludingVat = Money(grossAmountIncludingVat);
        LineDiscountAmountExcludingVat = Money(lineDiscountAmountExcludingVat);
        LineDiscountAmountIncludingVat = Money(lineDiscountAmountIncludingVat);
        InvoiceDiscountShareExcludingVat = Money(invoiceDiscountShareExcludingVat);
        InvoiceDiscountShareIncludingVat = Money(invoiceDiscountShareIncludingVat);
        TotalDiscountAmountExcludingVat = Money(totalDiscountAmountExcludingVat);
        TotalDiscountAmountIncludingVat = Money(totalDiscountAmountIncludingVat);
        NetAmountExcludingVat = Money(netAmountExcludingVat);
        VatAmount = Money(vatAmount);
        TotalAmountIncludingVat = Money(totalAmountIncludingVat);
        CostOfGoodsSold = 0m;
        GrossProfitExcludingVat = 0m;
        GrossProfitMargin = 0m;
    }

    // Burada mevcut stok altyapısının oluşturduğu negatif hareketi bu Accounting satırına bağlıyorum.
    public AccountingSalesOrderStockMovement LinkStockMovement(StockMovement stockMovement)
    {
        AccountingSalesOrder.EnsureDraft();
        if (stockMovement is null ||
            stockMovement.ProductVariantId != ProductVariantId ||
            stockMovement.Direction != StockMovementDirection.Out ||
            stockMovement.Type != StockMovementType.AccountingSale ||
            stockMovement.QuantityDelta >= 0)
        {
            throw new DomainException("A matching AccountingSale stock-out movement is required.");
        }

        if (_stockMovements.Any(link => link.StockMovementId == stockMovement.Id))
        {
            throw new DomainException("The stock movement is already linked to this sales order item.");
        }

        var linkedQuantity = _stockMovements.Sum(link => link.Quantity);
        var movementQuantity = checked(-stockMovement.QuantityDelta);
        if (linkedQuantity + movementQuantity > StockQuantity)
        {
            throw new DomainException("Linked stock-out quantity cannot exceed the sales stock quantity.");
        }

        var link = new AccountingSalesOrderStockMovement(this, stockMovement);
        _stockMovements.Add(link);
        return link;
    }

    // Burada gerçek FIFO tüketimlerinden satış maliyeti ile KDV hariç kâr ve marjı hesaplıyorum.
    public void ApplyProfitability()
    {
        AccountingSalesOrder.EnsureDraft();
        if (!HasCompleteStockEffect() || !HasCompleteCostConsumption())
        {
            throw new DomainException("Complete stock-out and FIFO consumption are required for profitability.");
        }

        CostOfGoodsSold = Money(
            _costLayerConsumptions.Sum(consumption => consumption.TotalCostExcludingVat));
        GrossProfitExcludingVat = Money(NetAmountExcludingVat - CostOfGoodsSold);
        GrossProfitMargin = CalculateMargin(GrossProfitExcludingVat, NetAmountExcludingVat);
    }

    // Burada satırın stok çıkış hareketlerinin satılan tam stok miktarını karşıladığını bildiriyorum.
    public bool HasCompleteStockEffect()
    {
        return _stockMovements.Sum(link => link.Quantity) == StockQuantity;
    }

    // Burada satırın FIFO tüketimlerinin satılan tam stok miktarını karşıladığını bildiriyorum.
    public bool HasCompleteCostConsumption()
    {
        return _costLayerConsumptions.Sum(consumption => consumption.Quantity) == StockQuantity;
    }

    // Burada maliyet katmanının bu satıra güvenle tüketim ekleyebileceğini önceden doğruluyorum.
    internal void EnsureCanRegisterConsumption(
        InventoryCostLayer layer,
        StockMovement stockMovement,
        int quantity)
    {
        AccountingSalesOrder.EnsureDraft();
        if (layer is null ||
            stockMovement is null ||
            quantity <= 0 ||
            layer.ProductVariantId != ProductVariantId ||
            stockMovement.ProductVariantId != ProductVariantId ||
            !_stockMovements.Any(link => link.StockMovementId == stockMovement.Id))
        {
            throw new DomainException("FIFO consumption must match the item, layer and linked stock movement.");
        }

        if (_costLayerConsumptions.Any(consumption =>
                consumption.InventoryCostLayerId == layer.Id &&
                consumption.StockMovementId == stockMovement.Id))
        {
            throw new DomainException("The cost layer is already consumed for this stock movement.");
        }

        if (_costLayerConsumptions.Sum(consumption => consumption.Quantity) + quantity > StockQuantity)
        {
            throw new DomainException("FIFO consumption cannot exceed the sales stock quantity.");
        }
    }

    // Burada doğrulanmış FIFO tüketimini satırın maliyet kaynağı koleksiyonuna ekliyorum.
    internal void RegisterConsumption(CostLayerConsumption consumption)
    {
        if (consumption is null || consumption.AccountingSalesOrderItemId != Id)
        {
            throw new DomainException("A matching cost layer consumption is required.");
        }

        _costLayerConsumptions.Add(consumption);
    }

    // Burada satış ve stok miktarlarının tam sayı stok sözleşmesiyle birebir uyumunu koruyorum.
    private static void EnsureQuantityContract(
        decimal quantity,
        decimal unitsPerSaleUnit,
        int stockQuantity)
    {
        var calculatedStockQuantity = quantity * unitsPerSaleUnit;
        if (quantity <= 0m ||
            unitsPerSaleUnit <= 0m ||
            stockQuantity <= 0 ||
            calculatedStockQuantity != decimal.Truncate(calculatedStockQuantity) ||
            calculatedStockQuantity > int.MaxValue ||
            calculatedStockQuantity != stockQuantity)
        {
            throw new DomainException(
                "Quantity and units per sale unit must produce the supplied positive whole stock quantity.");
        }
    }

    // Burada satır indiriminin ham tanım alanlarını birlikte ve tutarlı saklıyorum.
    private void SetLineDiscount(
        DiscountType? type,
        decimal? value,
        DiscountTaxBasis? taxBasis,
        DiscountUnitBasis? unitBasis)
    {
        if (!type.HasValue && !value.HasValue && !taxBasis.HasValue && !unitBasis.HasValue)
        {
            LineDiscountType = null;
            LineDiscountValue = null;
            LineDiscountTaxBasis = null;
            LineDiscountUnitBasis = null;
            return;
        }

        if (!type.HasValue ||
            !value.HasValue ||
            !taxBasis.HasValue ||
            type == DiscountType.FixedInvoiceTotal ||
            value.Value < 0m ||
            (type == DiscountType.FixedPerUnit && !unitBasis.HasValue) ||
            (type != DiscountType.FixedPerUnit && unitBasis.HasValue))
        {
            throw new DomainException("Line discount definition is incomplete or invalid.");
        }

        LineDiscountType = type;
        LineDiscountValue = value;
        LineDiscountTaxBasis = taxBasis;
        LineDiscountUnitBasis = unitBasis;
    }

    // Burada hesaplanan fiyat, indirim, KDV ve satır toplamlarının matematiksel tutarlılığını koruyorum.
    private static void EnsureCalculatedAmounts(
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
        var values = new[]
        {
            unitPriceExcludingVat,
            unitPriceIncludingVat,
            grossAmountExcludingVat,
            grossAmountIncludingVat,
            lineDiscountAmountExcludingVat,
            lineDiscountAmountIncludingVat,
            invoiceDiscountShareExcludingVat,
            invoiceDiscountShareIncludingVat,
            totalDiscountAmountExcludingVat,
            totalDiscountAmountIncludingVat,
            netAmountExcludingVat,
            vatAmount,
            totalAmountIncludingVat
        };
        if (values.Any(value => value < 0m) ||
            unitPriceExcludingVat < 0m ||
            unitPriceIncludingVat < 0m)
        {
            throw new DomainException("Calculated sales amounts and unit prices cannot be negative.");
        }

        if (Money(lineDiscountAmountExcludingVat + invoiceDiscountShareExcludingVat) !=
                Money(totalDiscountAmountExcludingVat) ||
            Money(lineDiscountAmountIncludingVat + invoiceDiscountShareIncludingVat) !=
                Money(totalDiscountAmountIncludingVat) ||
            Money(grossAmountExcludingVat - totalDiscountAmountExcludingVat) !=
                Money(netAmountExcludingVat) ||
            Money(netAmountExcludingVat + vatAmount) != Money(totalAmountIncludingVat) ||
            Money(grossAmountIncludingVat - totalDiscountAmountIncludingVat) !=
                Money(totalAmountIncludingVat))
        {
            throw new DomainException("Calculated sales amounts are not internally consistent.");
        }
    }

    // Burada zorunlu snapshot metnini temizleyip güvenli uzunlukta saklıyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        return NormalizeOptional(value, maximumLength, fieldName)
            ?? throw new DomainException($"{fieldName} cannot be empty.");
    }

    // Burada isteğe bağlı snapshot metnini boş veya güvenli uzunlukta saklıyorum.
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

    // Burada parasal toplamları Accounting para hassasiyetine yuvarlıyorum.
    private static decimal Money(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.InvoiceTotalScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada birim fiyatları Accounting birim fiyat hassasiyetine yuvarlıyorum.
    private static decimal UnitPrice(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada net gelir sıfırken bölme yapmadan satır brüt kâr marjını hesaplıyorum.
    private static decimal CalculateMargin(decimal grossProfit, decimal netAmountExcludingVat)
    {
        if (netAmountExcludingVat == 0m)
        {
            return 0m;
        }

        return decimal.Round(
            grossProfit / netAmountExcludingVat * 100m,
            AccountingPrecision.PercentageScale,
            AccountingPrecision.RoundingMode);
    }
}
