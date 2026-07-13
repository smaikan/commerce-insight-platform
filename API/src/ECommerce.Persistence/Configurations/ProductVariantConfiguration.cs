using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.Sku)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(variant => variant.Barcode)
            .HasMaxLength(100);

        builder.Property(variant => variant.Color)
            .HasMaxLength(80);

        builder.Property(variant => variant.Size)
            .HasMaxLength(80);

        builder.Property(variant => variant.Material)
            .HasMaxLength(120);

        builder.Property(variant => variant.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(variant => variant.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(variant => variant.DailyMetrics)
            .WithOne(metric => metric.ProductVariant)
            .HasForeignKey(metric => metric.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(variant => variant.InventoryTransactions)
            .WithOne(transaction => transaction.ProductVariant)
            .HasForeignKey(transaction => transaction.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(variant => variant.Sku)
            .IsUnique();

        builder.HasIndex(variant => variant.ProductId);
        builder.HasIndex(variant => variant.Barcode);
    }
}
