using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasKey(coupon => coupon.Id);

        builder.Property(coupon => coupon.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(coupon => coupon.Description)
            .HasMaxLength(1000);

        builder.Property(coupon => coupon.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(coupon => coupon.DiscountValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(coupon => coupon.MinimumOrderAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(coupon => coupon.Code)
            .IsUnique();
    }
}
