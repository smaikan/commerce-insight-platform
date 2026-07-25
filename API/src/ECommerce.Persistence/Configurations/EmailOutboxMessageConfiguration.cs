using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    // Burada genel e-posta outbox tablosunun kolon ve indeks kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("EmailOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasConversion<int>().IsRequired();
        builder.Property(message => message.DeduplicationKey).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Email).HasMaxLength(320).IsRequired();
        builder.Property(message => message.RecipientName).HasMaxLength(200);
        builder.Property(message => message.ProtectedToken).HasMaxLength(2000);
        builder.Property(message => message.OrderNumber).HasMaxLength(50);
        builder.Property(message => message.Amount).HasPrecision(18, 2);
        builder.Property(message => message.Status).HasMaxLength(100);
        builder.Property(message => message.ReturnNumber).HasMaxLength(50);
        builder.Property(message => message.LastError).HasMaxLength(1000);
        builder.Property(message => message.ProcessingWorker).HasMaxLength(128);
        builder.Property(message => message.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.HasIndex(message => message.DeduplicationKey).IsUnique();
        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.DeadLetteredAt,
            message.NextAttemptAt,
            message.LeaseExpiresAt
        });
    }
}
