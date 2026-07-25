using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    // Burada ödeme denemesi tablosunun tutar, retry anahtarı ve sağlayıcı işlem kimliği bütünlüğünü tanımlıyorum.
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0");
        });

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

        builder.Property(payment => payment.IdempotencyKey)
            .HasMaxLength(Payment.MaximumIdempotencyKeyLength);

        builder.Property(payment => payment.TransactionId)
            .HasMaxLength(150);

        builder.Property(payment => payment.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(payment => payment.OrderId);
        builder.HasIndex(payment => new { payment.OrderId, payment.IdempotencyKey })
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .IsUnique();
        builder.HasIndex(payment => new { payment.Provider, payment.TransactionId })
            .HasFilter("[TransactionId] IS NOT NULL")
            .IsUnique();
    }
}
