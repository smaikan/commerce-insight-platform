using ECommerce.Domain.Accounting.Common;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Accounting.CostLayers;

public sealed class CostLayerConsumption : BaseEntity
{
    public Guid InventoryCostLayerId { get; private set; }
    public InventoryCostLayer InventoryCostLayer { get; private set; } = null!;
    public Guid AccountingSalesOrderItemId { get; private set; }
    public AccountingSalesOrderItem AccountingSalesOrderItem { get; private set; } = null!;
    public Guid StockMovementId { get; private set; }
    public StockMovement StockMovement { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitCostExcludingVat { get; private set; }
    public decimal TotalCostExcludingVat { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un FIFO tüketimini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private CostLayerConsumption()
    {
    }

    // Burada tek maliyet katmanından gerçek satış stok hareketine ayrılan maliyeti değişmez kaydediyorum.
    internal CostLayerConsumption(
        InventoryCostLayer layer,
        AccountingSalesOrderItem item,
        StockMovement stockMovement,
        int quantity)
    {
        if (layer is null ||
            item is null ||
            stockMovement is null ||
            layer.Id == Guid.Empty ||
            item.Id == Guid.Empty ||
            stockMovement.Id == Guid.Empty ||
            quantity <= 0 ||
            layer.ProductVariantId != item.ProductVariantId ||
            layer.ProductVariantId != stockMovement.ProductVariantId ||
            stockMovement.QuantityDelta >= 0)
        {
            throw new DomainException(
                "A matching cost layer, sales item, stock-out movement and positive quantity are required.");
        }

        InventoryCostLayerId = layer.Id;
        InventoryCostLayer = layer;
        AccountingSalesOrderItemId = item.Id;
        AccountingSalesOrderItem = item;
        StockMovementId = stockMovement.Id;
        StockMovement = stockMovement;
        Quantity = quantity;
        UnitCostExcludingVat = decimal.Round(
            layer.UnitCostExcludingVat,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
        TotalCostExcludingVat = decimal.Round(
            UnitCostExcludingVat * quantity,
            AccountingPrecision.InvoiceTotalScale,
            AccountingPrecision.RoundingMode);
        CreatedAt = DateTime.UtcNow;
    }
}
