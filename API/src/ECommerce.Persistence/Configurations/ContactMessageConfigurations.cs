using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    // Burada iletişim aggregate'ının kolon, ilişki, concurrency ve yönetim indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.ReferenceNumber).HasMaxLength(ContactMessage.MaximumReferenceNumberLength).IsRequired();
        builder.Property(message => message.Name).HasMaxLength(ContactMessage.MaximumNameLength).IsRequired();
        builder.Property(message => message.Email).HasMaxLength(ContactMessage.MaximumEmailLength).IsRequired();
        builder.Property(message => message.Phone).HasMaxLength(ContactMessage.MaximumPhoneLength);
        builder.Property(message => message.Subject).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(message => message.ProvidedOrderNumber).HasMaxLength(ContactMessage.MaximumOrderNumberLength);
        builder.Property(message => message.Message).HasMaxLength(ContactMessage.MaximumMessageLength).IsRequired();
        builder.Property(message => message.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(message => message.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.Property(message => message.PrivacyNoticeVersion).HasMaxLength(ContactMessage.MaximumPrivacyNoticeVersionLength).IsRequired();
        builder.HasOne(message => message.User).WithMany().HasForeignKey(message => message.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(message => message.AssignedAdminUser).WithMany().HasForeignKey(message => message.AssignedAdminUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(message => message.VerifiedOrder).WithMany().HasForeignKey(message => message.VerifiedOrderId).OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(message => message.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(message => message.Replies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(message => message.ReferenceNumber).IsUnique();
        builder.HasIndex(message => new { message.Status, message.CreatedAt, message.Id });
        builder.HasIndex(message => new { message.Subject, message.CreatedAt, message.Id });
        builder.HasIndex(message => new { message.AssignedAdminUserId, message.Status, message.UpdatedAt });
        builder.HasIndex(message => new { message.UserId, message.CreatedAt });
        builder.HasIndex(message => message.ProvidedOrderNumber);
        builder.HasIndex(message => message.VerifiedOrderId);
        builder.HasIndex(message => new { message.AnonymizedAt, message.CreatedAt, message.Id });
    }
}

public sealed class ContactMessageActivityConfiguration : IEntityTypeConfiguration<ContactMessageActivity>
{
    // Burada append-only activity tablosunun alan, ilişki ve kronolojik indeksini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ContactMessageActivity> builder)
    {
        builder.ToTable("ContactMessageActivities");
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.Id).ValueGeneratedNever();
        builder.Property(activity => activity.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(activity => activity.Content).HasMaxLength(ContactMessage.MaximumNoteLength);
        builder.Property(activity => activity.PreviousValue).HasMaxLength(100);
        builder.Property(activity => activity.NewValue).HasMaxLength(100);
        builder.HasOne(activity => activity.ContactMessage).WithMany(message => message.Activities).HasForeignKey(activity => activity.ContactMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(activity => activity.ActorAdminUser).WithMany().HasForeignKey(activity => activity.ActorAdminUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(activity => new { activity.ContactMessageId, activity.CreatedAt, activity.Id });
    }
}

public sealed class ContactMessageReplyConfiguration : IEntityTypeConfiguration<ContactMessageReply>
{
    // Burada immutable reply kaydının body, idempotency ve outbox bağını tanımlıyorum.
    public void Configure(EntityTypeBuilder<ContactMessageReply> builder)
    {
        builder.ToTable("ContactMessageReplies");
        builder.HasKey(reply => reply.Id);
        builder.Property(reply => reply.Id).ValueGeneratedNever();
        builder.Property(reply => reply.Body).HasMaxLength(ContactMessage.MaximumReplyLength).IsRequired();
        builder.Property(reply => reply.IdempotencyKeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(reply => reply.RequestFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasOne(reply => reply.ContactMessage).WithMany(message => message.Replies).HasForeignKey(reply => reply.ContactMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(reply => reply.AdminUser).WithMany().HasForeignKey(reply => reply.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(reply => reply.OutboxMessage).WithMany().HasForeignKey(reply => reply.OutboxMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(reply => new { reply.ContactMessageId, reply.IdempotencyKeyHash }).IsUnique();
        builder.HasIndex(reply => reply.OutboxMessageId).IsUnique();
    }
}

public sealed class ContactSubmissionIdempotencyConfiguration : IEntityTypeConfiguration<ContactSubmissionIdempotency>
{
    // Burada submission idempotency hash, receipt ve bounded cleanup indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ContactSubmissionIdempotency> builder)
    {
        builder.ToTable("ContactSubmissionIdempotencies");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(record => record.RequestFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(record => record.ReferenceNumber).HasMaxLength(ContactMessage.MaximumReferenceNumberLength).IsRequired();
        builder.HasOne(record => record.ContactMessage).WithMany().HasForeignKey(record => record.ContactMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(record => record.KeyHash).IsUnique();
        builder.HasIndex(record => record.ExpiresAt);
    }
}
