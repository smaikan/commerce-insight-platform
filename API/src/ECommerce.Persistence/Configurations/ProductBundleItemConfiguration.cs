using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductBundleItemConfiguration : IEntityTypeConfiguration<ProductBundleItem>
{
    public void Configure(EntityTypeBuilder<ProductBundleItem> builder)
    {
        builder.ToTable("ProductBundleItems");

        builder.HasKey(item => item.Id);

        builder.HasOne(item => item.BundleProduct)
            .WithMany(product => product.BundleItems)
            .HasForeignKey(item => item.BundleProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.IncludedProduct)
            .WithMany()
            .HasForeignKey(item => item.IncludedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.BundleProductId, item.IncludedProductId })
            .IsUnique();
    }
}
