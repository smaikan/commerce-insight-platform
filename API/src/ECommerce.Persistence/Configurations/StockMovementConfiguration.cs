using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    // Burada stok hareketlerinin kolon, ilişki, bütünlük ve idempotency kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_QuantityDelta_NonZero",
                "[QuantityDelta] <> 0");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Direction_Matches_Delta",
                "([Direction] = 1 AND [QuantityDelta] > 0) OR ([Direction] = 2 AND [QuantityDelta] < 0)");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Stock_NonNegative",
                "[StockBeforeMovement] >= 0 AND [StockAfterMovement] >= 0");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Stock_Equation",
                "CAST([StockAfterMovement] AS bigint) = CAST([StockBeforeMovement] AS bigint) + CAST([QuantityDelta] AS bigint)");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Type_Valid",
                "[Type] IN (1, 10, 11, 20, 21, 22, 23, 30, 31, 40, 41, 42, 50, 51, 60)");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Type_Matches_Direction",
                "([Type] IN (1, 10, 21, 23, 50, 60) AND [Direction] = 1) OR " +
                "([Type] IN (11, 20, 22, 40, 41, 42, 51) AND [Direction] = 2) OR " +
                "[Type] IN (30, 31)");
            tableBuilder.HasCheckConstraint(
                "CK_StockMovements_Required_Reference",
                "([Type] NOT IN (20, 60) OR [OrderId] IS NOT NULL) AND " +
                "([Type] <> 21 OR [ReturnRequestId] IS NOT NULL) AND " +
                "([Type] NOT IN (22, 23) OR ([OrderId] IS NULL AND [ReturnRequestId] IS NULL))");
        });

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id)
            .ValueGeneratedNever();

        builder.Property(movement => movement.Direction)
            .IsRequired();

        builder.Property(movement => movement.Type)
            .IsRequired();

        builder.Property(movement => movement.QuantityDelta)
            .IsRequired();

        builder.Property(movement => movement.StockBeforeMovement)
            .IsRequired();

        builder.Property(movement => movement.StockAfterMovement)
            .IsRequired();

        builder.Property(movement => movement.Reason)
            .HasMaxLength(StockMovement.MaximumReasonLength);

        builder.HasOne(movement => movement.ProductVariant)
            .WithMany(variant => variant.StockMovements)
            .HasForeignKey(movement => movement.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(movement => movement.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReturnRequest>()
            .WithMany()
            .HasForeignKey(movement => movement.ReturnRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(movement => new { movement.ProductVariantId, movement.CreatedAt })
            .HasDatabaseName("IX_StockMovements_ProductVariantId_CreatedAt");

        builder.HasIndex(movement => new { movement.CreatedAt, movement.Id })
            .HasDatabaseName("IX_StockMovements_CreatedAt_Id");

        builder.HasIndex(movement => movement.OrderId)
            .HasDatabaseName("IX_StockMovements_OrderId");

        builder.HasIndex(movement => movement.ReturnRequestId)
            .HasDatabaseName("IX_StockMovements_ReturnRequestId");

        builder.HasIndex(movement => new
            {
                movement.OrderId,
                movement.ProductVariantId,
                movement.Type
            })
            .HasDatabaseName("UX_StockMovements_OrderId_ProductVariantId_Type")
            .IsUnique()
            .HasFilter("[OrderId] IS NOT NULL AND [ReturnRequestId] IS NULL AND [Type] IN (20, 60)");

        builder.HasIndex(movement => new
            {
                movement.ReturnRequestId,
                movement.ProductVariantId,
                movement.Type
            })
            .HasDatabaseName("UX_StockMovements_ReturnRequestId_ProductVariantId_Type")
            .IsUnique()
            .HasFilter("[ReturnRequestId] IS NOT NULL AND [Type] IN (20, 21)");
    }
}
