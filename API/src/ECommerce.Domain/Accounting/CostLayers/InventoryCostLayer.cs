using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Accounting.CostLayers;

public enum InventoryCostLayerSourceType
{
    PurchaseInvoiceAllocation = 1,
    OpeningBalance = 2
}

public enum CostLayerStatus
{
    Open = 1,
    Consumed = 2,
    Invalidated = 3
}

public sealed class InventoryCostLayer : BaseEntity
{
    private readonly List<CostLayerConsumption> _consumptions = [];
    private readonly List<CostLayerConsumptionReversal> _consumptionReversals = [];

    public InventoryCostLayerSourceType SourceType { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public Guid StockMovementId { get; private set; }
    public Guid? PurchaseInvoiceLineId { get; private set; }
    public PurchaseInvoiceLine? PurchaseInvoiceLine { get; private set; }
    public Guid? PurchaseInvoiceStockAllocationId { get; private set; }
    public PurchaseInvoiceStockAllocation? PurchaseInvoiceStockAllocation { get; private set; }
    public int OriginalQuantity { get; private set; }
    public int RemainingQuantity { get; private set; }
    public decimal UnitCostExcludingVat { get; private set; }
    public decimal UnitCostIncludingVat { get; private set; }
    public decimal TotalCostExcludingVat { get; private set; }
    public decimal TotalCostIncludingVat { get; private set; }
    public DateTime CostDate { get; private set; }
    public CostLayerStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<CostLayerConsumption> Consumptions => _consumptions.AsReadOnly();
    public IReadOnlyCollection<CostLayerConsumptionReversal> ConsumptionReversals => _consumptionReversals.AsReadOnly();

    // Burada EF Core'un maliyet katmanını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private InventoryCostLayer()
    {
    }

    // Burada yalnız onaylı allocation miktarı ve KDV hariç final maliyetle katmanı oluşturuyorum.
    public InventoryCostLayer(
        PurchaseInvoiceLine line,
        PurchaseInvoiceStockAllocation allocation,
        DateTime costDate)
    {
        if (line is null || allocation is null ||
            allocation.PurchaseInvoiceLineId != line.Id ||
            allocation.AllocatedQuantity <= 0)
        {
            throw new DomainException("A matching approved allocation is required for a cost layer.");
        }

        SourceType = InventoryCostLayerSourceType.PurchaseInvoiceAllocation;
        ProductVariantId = line.ProductVariantId;
        StockMovementId = allocation.StockMovementId;
        PurchaseInvoiceLineId = line.Id;
        PurchaseInvoiceLine = line;
        PurchaseInvoiceStockAllocationId = allocation.Id;
        PurchaseInvoiceStockAllocation = allocation;
        OriginalQuantity = allocation.AllocatedQuantity;
        RemainingQuantity = allocation.AllocatedQuantity;
        UnitCostExcludingVat = line.FinalUnitCostExcludingVat;
        UnitCostIncludingVat = line.FinalUnitCostIncludingVat;
        TotalCostExcludingVat = decimal.Round(
            UnitCostExcludingVat * OriginalQuantity,
            2,
            MidpointRounding.AwayFromZero);
        TotalCostIncludingVat = decimal.Round(
            UnitCostIncludingVat * OriginalQuantity,
            2,
            MidpointRounding.AwayFromZero);
        CostDate = costDate;
        Status = CostLayerStatus.Open;
        ConcurrencyToken = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Burada yeni varyantın gerçek OpeningBalance hareketinden opsiyonel maliyetli ve allocation gerektirmeyen katmanı oluşturuyorum.
    public InventoryCostLayer(
        ProductVariant productVariant,
        StockMovement openingBalanceMovement,
        decimal unitCostExcludingVat = 0m,
        decimal? unitCostIncludingVat = null)
    {
        ArgumentNullException.ThrowIfNull(productVariant);
        ArgumentNullException.ThrowIfNull(openingBalanceMovement);

        if (productVariant.Id == Guid.Empty ||
            openingBalanceMovement.Id == Guid.Empty ||
            openingBalanceMovement.ProductVariantId != productVariant.Id ||
            openingBalanceMovement.Type != StockMovementType.OpeningBalance ||
            openingBalanceMovement.Direction != StockMovementDirection.In ||
            openingBalanceMovement.QuantityDelta <= 0 ||
            openingBalanceMovement.StockBeforeMovement != 0 ||
            openingBalanceMovement.StockAfterMovement != openingBalanceMovement.QuantityDelta ||
            !productVariant.StockMovements.Any(item => item.Id == openingBalanceMovement.Id))
        {
            throw new DomainException(
                "A matching positive OpeningBalance stock movement is required for an opening cost layer.");
        }

        var resolvedUnitCostIncludingVat =
            unitCostIncludingVat ?? unitCostExcludingVat;
        if (unitCostExcludingVat < 0m || resolvedUnitCostIncludingVat < 0m)
        {
            throw new DomainException("Opening unit costs cannot be negative.");
        }

        SourceType = InventoryCostLayerSourceType.OpeningBalance;
        ProductVariantId = productVariant.Id;
        StockMovementId = openingBalanceMovement.Id;
        OriginalQuantity = openingBalanceMovement.QuantityDelta;
        RemainingQuantity = openingBalanceMovement.QuantityDelta;
        UnitCostExcludingVat = decimal.Round(
            unitCostExcludingVat,
            4,
            MidpointRounding.AwayFromZero);
        // Burada KDV dahil değer verilmediyse birincil stok değerleme maliyeti olan KDV hariç değeri kullanıyorum.
        UnitCostIncludingVat = decimal.Round(
            resolvedUnitCostIncludingVat,
            4,
            MidpointRounding.AwayFromZero);
        TotalCostExcludingVat = decimal.Round(
            UnitCostExcludingVat * OriginalQuantity,
            2,
            MidpointRounding.AwayFromZero);
        TotalCostIncludingVat = decimal.Round(
            UnitCostIncludingVat * OriginalQuantity,
            2,
            MidpointRounding.AwayFromZero);
        CostDate = openingBalanceMovement.CreatedAt;
        Status = CostLayerStatus.Open;
        ConcurrencyToken = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Burada açık katmanları CostDate, CreatedAt ve Id sırasıyla gerçek FIFO tüketimine hazırlıyorum.
    public static IReadOnlyList<InventoryCostLayer> OrderForFifo(
        IEnumerable<InventoryCostLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        return layers
            .Where(layer => layer.CanBeConsumed())
            .OrderBy(layer => layer.CostDate)
            .ThenBy(layer => layer.CreatedAt)
            .ThenBy(layer => layer.Id)
            .ToArray();
    }

    // Burada katmanın pozitif miktarla FIFO tüketimine açık olup olmadığını bildiriyorum.
    public bool CanBeConsumed()
    {
        return Status == CostLayerStatus.Open && RemainingQuantity > 0;
    }

    // Burada yalnız OpeningBalance katmanının henüz tüketilmemiş miktarına uygulanacak gelecekteki birim maliyeti eşzamanlılık anahtarıyla güncelliyorum.
    public void UpdateOpeningBalanceRemainingCost(
        decimal unitCostExcludingVat,
        decimal unitCostIncludingVat,
        Guid expectedConcurrencyToken)
    {
        if (SourceType != InventoryCostLayerSourceType.OpeningBalance)
        {
            throw new DomainException(
                "Only an OpeningBalance cost layer can receive an opening cost update.");
        }

        if (!CanBeConsumed())
        {
            throw new DomainException(
                "A fully consumed opening cost layer cannot be revalued.");
        }

        if (expectedConcurrencyToken == Guid.Empty ||
            expectedConcurrencyToken != ConcurrencyToken)
        {
            throw new DomainException(
                "The opening cost layer concurrency token is stale.");
        }

        if (unitCostExcludingVat < 0m || unitCostIncludingVat < 0m)
        {
            throw new DomainException("Opening unit costs cannot be negative.");
        }

        UnitCostExcludingVat = decimal.Round(
            unitCostExcludingVat,
            4,
            MidpointRounding.AwayFromZero);
        UnitCostIncludingVat = decimal.Round(
            unitCostIncludingVat,
            4,
            MidpointRounding.AwayFromZero);
        TotalCostExcludingVat = decimal.Round(
            UnitCostExcludingVat * RemainingQuantity,
            2,
            MidpointRounding.AwayFromZero);
        TotalCostIncludingVat = decimal.Round(
            UnitCostIncludingVat * RemainingQuantity,
            2,
            MidpointRounding.AwayFromZero);
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada gerçek satış stok hareketi için miktarı düşürüp değişmez FIFO tüketim kaydını birlikte oluşturuyorum.
    public CostLayerConsumption Consume(
        AccountingSalesOrderItem item,
        StockMovement stockMovement,
        int quantity)
    {
        if (!CanBeConsumed() || quantity <= 0 || quantity > RemainingQuantity)
        {
            throw new DomainException("The cost layer does not contain enough remaining quantity.");
        }

        if (item is null ||
            stockMovement is null ||
            ProductVariantId != item.ProductVariantId ||
            ProductVariantId != stockMovement.ProductVariantId)
        {
            throw new DomainException("The cost layer must match the sales item and stock movement variant.");
        }

        item.EnsureCanRegisterConsumption(this, stockMovement, quantity);
        var consumption = new CostLayerConsumption(this, item, stockMovement, quantity);
        RemainingQuantity -= quantity;
        Status = RemainingQuantity == 0
            ? CostLayerStatus.Consumed
            : CostLayerStatus.Open;
        ConcurrencyToken = Guid.NewGuid();
        _consumptions.Add(consumption);
        item.RegisterConsumption(consumption);
        return consumption;
    }

    public CostLayerConsumptionReversal Restore(
        CostLayerConsumption consumption,
        StockMovement reversalMovement,
        Guid accountingSalesOrderId,
        long reversedBy,
        DateTime reversedAt,
        string reason)
    {
        if (consumption is null || reversalMovement is null ||
            consumption.InventoryCostLayerId != Id ||
            reversalMovement.ProductVariantId != ProductVariantId ||
            reversalMovement.Type != StockMovementType.AccountingSaleCancellation ||
            reversalMovement.QuantityDelta < consumption.Quantity ||
            accountingSalesOrderId == Guid.Empty || reversedBy <= 0 || reversedAt == default)
        {
            throw new DomainException("A matching FIFO consumption and accounting cancellation movement are required.");
        }

        if (_consumptionReversals.Any(item => item.CostLayerConsumptionId == consumption.Id))
        {
            throw new DomainException("The FIFO consumption is already reversed.");
        }

        if (RemainingQuantity + consumption.Quantity > OriginalQuantity)
        {
            throw new DomainException("FIFO restoration cannot exceed the original layer quantity.");
        }

        var reversal = new CostLayerConsumptionReversal(
            this, consumption, reversalMovement, accountingSalesOrderId, reversedBy, reversedAt, reason);
        RemainingQuantity += consumption.Quantity;
        Status = CostLayerStatus.Open;
        ConcurrencyToken = Guid.NewGuid();
        _consumptionReversals.Add(reversal);
        return reversal;
    }

    public void InvalidateUnconsumedPurchaseLayer()
    {
        if (SourceType != InventoryCostLayerSourceType.PurchaseInvoiceAllocation ||
            RemainingQuantity != OriginalQuantity || _consumptions.Count != 0)
            throw new DomainException("A consumed purchase cost layer requires an approved retroactive cost policy.");
        Status = CostLayerStatus.Invalidated;
        RemainingQuantity = 0;
        ConcurrencyToken = Guid.NewGuid();
    }
}

public sealed class ProductVariantCostHistory : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public ProductVariantCostHistorySourceType SourceType { get; private set; }
    public decimal? PreviousCostExcludingVat { get; private set; }
    public decimal NewCostExcludingVat { get; private set; }
    public decimal? PreviousCostIncludingVat { get; private set; }
    public decimal NewCostIncludingVat { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int OpeningStockQuantity { get; private set; }
    public int? ClosingStockQuantity { get; private set; }
    public Guid SourceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un maliyet geçmişini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductVariantCostHistory()
    {
    }

    // Burada varyantın raporlama amaçlı yeni etkin maliyet snapshot'ını açıyorum.
    public ProductVariantCostHistory(
        Guid productVariantId,
        decimal? previousCostExcludingVat,
        decimal newCostExcludingVat,
        decimal? previousCostIncludingVat,
        decimal newCostIncludingVat,
        DateTime validFrom,
        int openingStockQuantity,
        Guid sourceId,
        ProductVariantCostHistorySourceType sourceType)
    {
        if (productVariantId == Guid.Empty || sourceId == Guid.Empty ||
            !Enum.IsDefined(sourceType) ||
            previousCostExcludingVat is < 0m ||
            previousCostIncludingVat is < 0m ||
            newCostExcludingVat < 0m || newCostIncludingVat < 0m ||
            openingStockQuantity < 0 || validFrom == default)
        {
            throw new DomainException("Valid variant, cost, date, stock and source values are required.");
        }

        ProductVariantId = productVariantId;
        SourceType = sourceType;
        PreviousCostExcludingVat = RoundOptionalCost(previousCostExcludingVat);
        NewCostExcludingVat = RoundCost(newCostExcludingVat);
        PreviousCostIncludingVat = RoundOptionalCost(previousCostIncludingVat);
        NewCostIncludingVat = RoundCost(newCostIncludingVat);
        ValidFrom = validFrom;
        OpeningStockQuantity = openingStockQuantity;
        SourceId = sourceId;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada önceki etkin maliyet geçmişini yeni maliyet tarihi ve stok snapshot'ıyla kapatıyorum.
    public void Close(DateTime validTo, int closingStockQuantity)
    {
        if (ValidTo.HasValue || validTo < ValidFrom || closingStockQuantity < 0)
        {
            throw new DomainException("Cost history cannot be closed with the supplied values.");
        }

        ValidTo = validTo;
        ClosingStockQuantity = closingStockQuantity;
    }

    // Burada maliyet geçmişindeki birim maliyeti kalıcı dört ondalık hassasiyete yuvarlıyorum.
    private static decimal RoundCost(decimal value)
    {
        return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    // Burada önceki opsiyonel birim maliyeti varsa aynı kalıcı hassasiyete yuvarlıyorum.
    private static decimal? RoundOptionalCost(decimal? value)
    {
        return value.HasValue ? RoundCost(value.Value) : null;
    }
}
