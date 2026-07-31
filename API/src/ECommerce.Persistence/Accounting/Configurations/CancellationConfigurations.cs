using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.SalesOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Accounting.Configurations;

public sealed class CostLayerConsumptionReversalConfiguration : IEntityTypeConfiguration<CostLayerConsumptionReversal>
{
    public void Configure(EntityTypeBuilder<CostLayerConsumptionReversal> b)
    {
        b.ToTable("AccountingCostLayerConsumptionReversals", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingCostLayerConsumptionReversals_Quantity",
                "[Quantity] > 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TotalCostExcludingVat).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.CostLayerConsumption).WithOne().HasForeignKey<CostLayerConsumptionReversal>(x => x.CostLayerConsumptionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InventoryCostLayer).WithMany(x => x.ConsumptionReversals).HasForeignKey(x => x.InventoryCostLayerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AccountingSalesOrder>().WithMany().HasForeignKey(x => x.AccountingSalesOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StockMovement).WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CostLayerConsumptionId).IsUnique();
        b.HasIndex(x => x.AccountingSalesOrderId);
    }
}

public sealed class AccountingSalesOrderStockMovementReversalConfiguration : IEntityTypeConfiguration<AccountingSalesOrderStockMovementReversal>
{
    public void Configure(EntityTypeBuilder<AccountingSalesOrderStockMovementReversal> b)
    {
        b.ToTable("AccountingSalesOrderStockMovementReversals", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingSalesOrderStockMovementReversals_Quantity",
                "[Quantity] > 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne(x => x.AccountingSalesOrder).WithMany().HasForeignKey(x => x.AccountingSalesOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OriginalStockMovement).WithMany().HasForeignKey(x => x.OriginalStockMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReversalStockMovement).WithMany().HasForeignKey(x => x.ReversalStockMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OriginalStockMovementId).IsUnique();
        b.HasIndex(x => x.ReversalStockMovementId).IsUnique();
    }
}
