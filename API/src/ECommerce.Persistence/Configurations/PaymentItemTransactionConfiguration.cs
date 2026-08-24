using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class PaymentItemTransactionConfiguration : IEntityTypeConfiguration<PaymentItemTransaction>
{
    // Burada CF-Retrieve item snapshot'larının app-generated kimlik, FK ve provider uniqueness kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<PaymentItemTransaction> builder)
    {
        builder.ToTable("PaymentItemTransactions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_PaymentItemTransactions_Amounts_Positive", "[Price] > 0 AND [PaidPrice] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ProviderTransactionId)
            .HasMaxLength(PaymentItemTransaction.MaximumProviderTransactionIdLength)
            .IsRequired();
        builder.Property(item => item.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.PaidPrice).HasPrecision(18, 2).IsRequired();
        builder.HasOne(item => item.Payment).WithMany(payment => payment.ItemTransactions).HasForeignKey(item => item.PaymentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.OrderItem).WithMany().HasForeignKey(item => item.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PaymentId, item.OrderItemId }).IsUnique();
        builder.HasIndex(item => item.ProviderTransactionId).IsUnique();
    }
}
