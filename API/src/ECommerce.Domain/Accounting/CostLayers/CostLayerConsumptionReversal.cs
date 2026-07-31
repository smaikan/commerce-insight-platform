using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Accounting.CostLayers;

public sealed class CostLayerConsumptionReversal : BaseEntity
{
    public Guid CostLayerConsumptionId { get; private set; }
    public CostLayerConsumption CostLayerConsumption { get; private set; } = null!;
    public Guid InventoryCostLayerId { get; private set; }
    public InventoryCostLayer InventoryCostLayer { get; private set; } = null!;
    public Guid AccountingSalesOrderId { get; private set; }
    public Guid StockMovementId { get; private set; }
    public StockMovement StockMovement { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal TotalCostExcludingVat { get; private set; }
    public long ReversedBy { get; private set; }
    public DateTime ReversedAt { get; private set; }
    public string Reason { get; private set; } = null!;

    private CostLayerConsumptionReversal() { }

    internal CostLayerConsumptionReversal(
        InventoryCostLayer layer,
        CostLayerConsumption consumption,
        StockMovement stockMovement,
        Guid accountingSalesOrderId,
        long reversedBy,
        DateTime reversedAt,
        string reason)
    {
        if (stockMovement.Type != StockMovementType.AccountingSaleCancellation ||
            string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A cancellation movement and reason are required.");
        }

        InventoryCostLayerId = layer.Id;
        InventoryCostLayer = layer;
        CostLayerConsumptionId = consumption.Id;
        CostLayerConsumption = consumption;
        AccountingSalesOrderId = accountingSalesOrderId;
        StockMovementId = stockMovement.Id;
        StockMovement = stockMovement;
        Quantity = consumption.Quantity;
        TotalCostExcludingVat = consumption.TotalCostExcludingVat;
        ReversedBy = reversedBy;
        ReversedAt = reversedAt;
        Reason = reason.Trim();
    }
}
