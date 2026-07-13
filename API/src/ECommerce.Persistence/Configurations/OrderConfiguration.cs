using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

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

        builder.HasOne(order => order.Address)
            .WithMany()
            .HasForeignKey(order => order.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(order => order.Payments)
            .WithOne(payment => payment.Order)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.Status);
    }
}
