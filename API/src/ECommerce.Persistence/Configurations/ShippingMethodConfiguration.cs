using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    // Burada kargo yöntemi tablosunun alan, ücret ve sıralama kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethods", table =>
        {
            table.HasCheckConstraint(
                "CK_ShippingMethods_FixedFee_NonNegative",
                "CAST([FixedFee] AS REAL) >= 0");
            table.HasCheckConstraint(
                "CK_ShippingMethods_DisplayOrder_NonNegative",
                "[DisplayOrder] >= 0");
        });

        builder.HasKey(shippingMethod => shippingMethod.Id);

        builder.Property(shippingMethod => shippingMethod.Name)
            .HasMaxLength(ShippingMethod.MaximumNameLength)
            .IsRequired();

        builder.Property(shippingMethod => shippingMethod.FixedFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(shippingMethod => shippingMethod.IsActive)
            .IsRequired();

        builder.Property(shippingMethod => shippingMethod.DisplayOrder)
            .IsRequired();

        builder.HasIndex(shippingMethod => shippingMethod.Name)
            .IsUnique();

        builder.HasIndex(shippingMethod => new { shippingMethod.IsActive, shippingMethod.DisplayOrder });
    }
}
