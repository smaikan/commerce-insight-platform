using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ContactSubmissionIdempotency : BaseEntity
{
    public string KeyHash { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public Guid ContactMessageId { get; private set; }
    public ContactMessage ContactMessage { get; private set; } = null!;
    public string ReferenceNumber { get; private set; } = null!;
    public DateTime SubmittedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Burada EF Core'un idempotency kaydını yükleyebilmesi için boş kurucuyu tutuyorum.
    private ContactSubmissionIdempotency()
    {
    }

    // Burada hashlenmiş anahtarı değişmez public receipt sonucuna bağlıyorum.
    public ContactSubmissionIdempotency(string keyHash, string requestFingerprint, ContactMessage message, DateTime expiresAt)
    {
        if (message is null || string.IsNullOrEmpty(keyHash) || keyHash.Length != 64 ||
            string.IsNullOrEmpty(requestFingerprint) || requestFingerprint.Length != 64 || expiresAt <= message.CreatedAt)
        {
            throw new DomainException("Contact idempotency values are invalid.");
        }

        KeyHash = keyHash;
        RequestFingerprint = requestFingerprint;
        ContactMessageId = message.Id;
        ContactMessage = message;
        ReferenceNumber = message.ReferenceNumber;
        SubmittedAt = message.CreatedAt;
        ExpiresAt = expiresAt;
    }
}
