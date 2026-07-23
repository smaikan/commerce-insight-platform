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
        builder.Property(message => message.Email).HasMaxLength(320).IsRequired();
        builder.Property(message => message.RecipientName).HasMaxLength(200);
        builder.Property(message => message.ProtectedToken).HasMaxLength(2000);
        builder.Property(message => message.LastError).HasMaxLength(1000);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt });
    }
}
