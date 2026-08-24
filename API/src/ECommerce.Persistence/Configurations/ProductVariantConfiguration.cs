using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    // Burada varyant tablosunun ürün ilişkisi, stok, fiyat ve benzersizlik kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(variant => variant.Id);
        builder.HasAlternateKey(variant => new
        {
            variant.Id,
            variant.ProductId
        });

        builder.Property(variant => variant.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(variant => variant.Value)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(variant => variant.Sku)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(variant => variant.Barcode)
            .HasMaxLength(100);

        builder.Property(variant => variant.Material)
            .HasMaxLength(120);

        builder.Property(variant => variant.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(variant => variant.NetPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(variant => variant.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(variant => variant.ConcurrencyToken)
            .IsConcurrencyToken();

        builder.Property(variant => variant.DeletedAtUtc);

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(variant => variant.VariantOptionName)
            .WithMany(optionName => optionName.ProductVariants)
            .HasForeignKey(variant => variant.VariantOptionNameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(variant => variant.VariantOptionValue)
            .WithMany(optionValue => optionValue.ProductVariants)
            .HasForeignKey(variant => variant.VariantOptionValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(variant => variant.DailyMetrics)
            .WithOne(metric => metric.ProductVariant)
            .HasForeignKey(metric => metric.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(variant => variant.StockMovements)
            .WithOne(movement => movement.ProductVariant)
            .HasForeignKey(movement => movement.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(variant => variant.StockMovements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(variant => variant.Sku)
            .IsUnique()
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.HasIndex(variant => variant.ProductId);
        builder.HasIndex(variant => variant.DeletedAtUtc);
        builder.HasIndex(variant => variant.VariantOptionNameId);
        builder.HasIndex(variant => variant.VariantOptionValueId);
        builder.HasIndex(variant => variant.Barcode);
    }
}
