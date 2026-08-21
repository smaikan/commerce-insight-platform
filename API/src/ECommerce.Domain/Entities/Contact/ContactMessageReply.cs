using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ContactMessageReply : BaseEntity
{
    public Guid ContactMessageId { get; private set; }
    public ContactMessage ContactMessage { get; private set; } = null!;
    public long AdminUserId { get; private set; }
    public User AdminUser { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string IdempotencyKeyHash { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public Guid OutboxMessageId { get; private set; }
    public EmailOutboxMessage OutboxMessage { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un immutable reply kaydını yükleyebilmesi için boş kurucuyu tutuyorum.
    private ContactMessageReply()
    {
    }

    // Burada yanıt intent'ini hash ve outbox bağıyla immutable olarak oluşturuyorum.
    internal ContactMessageReply(ContactMessage contactMessage, long adminUserId, string body, string keyHash, string requestFingerprint, EmailOutboxMessage outboxMessage, DateTime utcNow)
    {
        if (adminUserId <= 0 || outboxMessage is null || string.IsNullOrEmpty(keyHash) || keyHash.Length != 64 ||
            string.IsNullOrEmpty(requestFingerprint) || requestFingerprint.Length != 64)
        {
            throw new DomainException("Contact reply identity values are invalid.");
        }

        ContactMessageId = contactMessage.Id;
        ContactMessage = contactMessage;
        AdminUserId = adminUserId;
        Body = body;
        IdempotencyKeyHash = keyHash;
        RequestFingerprint = requestFingerprint;
        OutboxMessageId = outboxMessage.Id;
        OutboxMessage = outboxMessage;
        CreatedAt = utcNow;
    }

    // Burada reply audit bağını koruyup müşteri yazışma gövdesini retention kapsamında siliyorum.
    internal void RedactBodyForRetention() => Body = "[Anonymized by retention policy]";
}
