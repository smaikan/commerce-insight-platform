using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    // Burada sipariş kalemi tablosunun kolon, ilişki ve sorgu indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
            tableBuilder.HasCheckConstraint("CK_OrderItems_UnitPrice_Positive", "[UnitPrice] > 0");
            tableBuilder.HasCheckConstraint("CK_OrderItems_TotalPrice_Positive", "[TotalPrice] > 0");
            tableBuilder.HasCheckConstraint("CK_OrderItems_Discount_Within_Total", "[DiscountTotal] >= 0 AND [DiscountTotal] <= [TotalPrice]");
            tableBuilder.HasCheckConstraint("CK_OrderItems_Tax_NonNegative", "[TaxTotal] >= 0 AND ([TaxRatePercentage] IS NULL OR ([TaxRatePercentage] >= 0 AND [TaxRatePercentage] <= 100))");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductTitleSnapshot)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(item => item.VariantSkuSnapshot)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.DiscountTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.TaxRatePercentage)
            .HasPrecision(5, 2);

        builder.Property(item => item.TaxTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => new { item.ProductVariantId, item.ProductId })
            .HasPrincipalKey(variant => new { variant.Id, variant.ProductId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.OrderId);
        builder.HasIndex(item => new { item.OrderId, item.ProductId });
        builder.HasIndex(item => new { item.OrderId, item.ProductVariantId }).IsUnique();
    }
}
