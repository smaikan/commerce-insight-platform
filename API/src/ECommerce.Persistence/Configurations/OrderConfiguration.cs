using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    // Burada sipariş tablosunun kolon, ilişki ve sorgu indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Orders_Totals_NonNegative", "[SubTotal] >= 0 AND [DiscountTotal] >= 0 AND [ShippingTotal] >= 0 AND [TaxTotal] >= 0 AND [GrandTotal] >= 0");
            tableBuilder.HasCheckConstraint("CK_Orders_Discount_Within_SubTotal", "[DiscountTotal] <= [SubTotal]");
        });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.SubTotal).HasPrecision(18, 2);
        builder.Property(order => order.DiscountTotal).HasPrecision(18, 2);
        builder.Property(order => order.ShippingTotal).HasPrecision(18, 2);
        builder.Property(order => order.TaxTotal).HasPrecision(18, 2);
        builder.Property(order => order.GrandTotal).HasPrecision(18, 2);
        builder.Property(order => order.CouponCode).HasMaxLength(Order.MaximumCouponCodeLength);
        builder.Property(order => order.ShippingMethodName).HasMaxLength(ShippingMethod.MaximumNameLength);
        builder.Property(order => order.ReservationExpiresAt);
        builder.Property(order => order.ShippingCarrier).HasMaxLength(Order.MaximumShippingCarrierLength);
        builder.Property(order => order.TrackingNumber).HasMaxLength(Order.MaximumTrackingNumberLength);
        builder.Property(order => order.TrackingUrl).HasMaxLength(Order.MaximumTrackingUrlLength);
        builder.Property(order => order.ShippedAt);
        builder.Property(order => order.DeliveredAt);

        builder.HasOne(order => order.Address)
            .WithMany()
            .HasForeignKey(order => order.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.ShippingMethod)
            .WithMany()
            .HasForeignKey(order => order.ShippingMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(order => order.Payments)
            .WithOne(payment => payment.Order)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(order => order.AddressSnapshots)
            .WithOne(snapshot => snapshot.Order)
            .HasForeignKey(snapshot => snapshot.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(order => order.CustomerSnapshot)
            .WithOne(snapshot => snapshot.Order)
            .HasForeignKey<OrderCustomerSnapshot>(snapshot => snapshot.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => new { order.Status, order.ReservationExpiresAt });
        builder.HasIndex(order => new { order.UserId, order.Status });
        builder.HasIndex(order => new { order.UserId, order.CreatedAt });
        builder.HasIndex(order => order.ShippingMethodId);
    }
}
