using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsages");

        builder.HasKey(usage => usage.Id);

        builder.HasOne(usage => usage.Coupon)
            .WithMany()
            .HasForeignKey(usage => usage.CouponId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(usage => usage.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(usage => new { usage.CouponId, usage.UserId, usage.OrderId });
    }
}
