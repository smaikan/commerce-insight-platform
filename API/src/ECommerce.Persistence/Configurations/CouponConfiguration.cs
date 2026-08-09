using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    // Burada kupon tablosunun alan sınırlarını, kullanım bütünlüğünü ve sorgu indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Coupons_UsedCount_NonNegative", "[UsedCount] >= 0");
            tableBuilder.HasCheckConstraint("CK_Coupons_DiscountValue_Positive", "[DiscountValue] > 0");
            tableBuilder.HasCheckConstraint("CK_Coupons_UsageLimit_Positive", "[UsageLimit] IS NULL OR [UsageLimit] > 0");
        });

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

        builder.Property(coupon => coupon.IsMemberOnly)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(coupon => coupon.Code)
            .IsUnique();
        builder.HasIndex(coupon => new { coupon.IsActive, coupon.StartsAt, coupon.ExpiresAt });
    }
}
