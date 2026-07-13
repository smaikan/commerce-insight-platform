using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.OrderId);
    }
}
