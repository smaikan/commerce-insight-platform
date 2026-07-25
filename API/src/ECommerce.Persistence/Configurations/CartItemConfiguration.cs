using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    // Burada sepet satırının ilişki, para, adet ve veri bütünlüğü kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_CartItems_Quantity_Positive",
                "[Quantity] > 0");
            tableBuilder.HasCheckConstraint(
                "CK_CartItems_UnitPrice_Positive",
                "CAST([UnitPrice] AS DECIMAL(18,2)) > 0");
        });

        builder.HasKey(item => item.Id);

        builder.Ignore(item => item.TotalPrice);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => new
            {
                item.ProductVariantId,
                item.ProductId
            })
            .HasPrincipalKey(variant => new
            {
                variant.Id,
                variant.ProductId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CartId, item.ProductVariantId })
            .IsUnique();
    }
}
