using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    // Burada iade kalemi tablosunun snapshot, miktar, ürün ilişkisi ve tekillik kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable("ReturnItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_ReturnItems_Quantity_Positive", "[Quantity] > 0");
            tableBuilder.HasCheckConstraint("CK_ReturnItems_UnitPrice_Positive", "[UnitPrice] > 0");
            tableBuilder.HasCheckConstraint("CK_ReturnItems_LineTotal_Positive", "[LineTotal] > 0");
            tableBuilder.HasCheckConstraint("CK_ReturnItems_RefundTotal_NonNegative", "[RefundTotal] >= 0");
            tableBuilder.HasCheckConstraint(
                "CK_ReturnItems_SalesMetricReversedQuantity",
                "[SalesMetricReversedQuantity] >= 0 AND [SalesMetricReversedQuantity] <= [Quantity]");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductTitleSnapshot)
            .HasMaxLength(ReturnItem.MaximumProductTitleLength)
            .IsRequired();

        builder.Property(item => item.VariantSkuSnapshot)
            .HasMaxLength(ReturnItem.MaximumVariantSkuLength)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.RefundTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.SalesMetricReversedQuantity)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasOne(item => item.OrderItem)
            .WithMany()
            .HasForeignKey(item => item.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => new { item.ProductVariantId, item.ProductId })
            .HasPrincipalKey(variant => new { variant.Id, variant.ProductId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => new { item.ReplacementProductVariantId, item.ProductId })
            .HasPrincipalKey(variant => new { variant.Id, variant.ProductId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.ReturnRequestId);
        builder.HasIndex(item => new { item.ReturnRequestId, item.OrderItemId }).IsUnique();
        builder.HasIndex(item => item.OrderItemId);
        builder.HasIndex(item => item.ReplacementProductVariantId);
    }
}
