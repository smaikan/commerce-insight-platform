using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(item => item.Id);

        builder.Ignore(item => item.TotalPrice);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CartId, item.ProductVariantId })
            .IsUnique();
    }
}
