using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Provider)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.TransactionId)
            .HasMaxLength(150);

        builder.Property(payment => payment.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(payment => payment.OrderId);
        builder.HasIndex(payment => payment.TransactionId);
    }
}
