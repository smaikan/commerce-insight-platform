using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class ContactMessage : AuditableEntity
{
    public const int MaximumReferenceNumberLength = 32;
    public const int MaximumNameLength = 150;
    public const int MaximumEmailLength = 320;
    public const int MaximumPhoneLength = 30;
    public const int MaximumOrderNumberLength = 50;
    public const int MaximumMessageLength = 5_000;
    public const int MaximumNoteLength = 2_000;
    public const int MaximumReplyLength = 5_000;
    public const int MaximumPrivacyNoticeVersionLength = 50;

    private readonly List<ContactMessageActivity> _activities = [];
    private readonly List<ContactMessageReply> _replies = [];

    public string ReferenceNumber { get; private set; } = null!;
    public long? UserId { get; private set; }
    public User? User { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public ContactMessageSubject Subject { get; private set; }
    public string? ProvidedOrderNumber { get; private set; }
    public Guid? VerifiedOrderId { get; private set; }
    public Order? VerifiedOrder { get; private set; }
    public string Message { get; private set; } = null!;
    public ContactMessageStatus Status { get; private set; }
    public long? AssignedAdminUserId { get; private set; }
    public User? AssignedAdminUser { get; private set; }
    public DateTime? FirstRespondedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public string PrivacyNoticeVersion { get; private set; } = null!;
    public DateTime PrivacyNoticePublishedAt { get; private set; }
    public DateTime? AnonymizedAt { get; private set; }
    public IReadOnlyCollection<ContactMessageActivity> Activities => _activities.AsReadOnly();
    public IReadOnlyCollection<ContactMessageReply> Replies => _replies.AsReadOnly();

    // Burada EF Core'un aggregate'ı veritabanından yükleyebilmesi için boş kurucuyu tutuyorum.
    private ContactMessage()
    {
    }

    // Burada doğrulanmış ve normalize edilmiş public iletişim başvurusunu oluşturuyorum.
    public ContactMessage(
        string referenceNumber,
        long? userId,
        string name,
        string email,
        string? phone,
        ContactMessageSubject subject,
        string? providedOrderNumber,
        Guid? verifiedOrderId,
        string message,
        string privacyNoticeVersion,
        DateTime privacyNoticePublishedAt,
        DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        EnsureUtc(privacyNoticePublishedAt, nameof(privacyNoticePublishedAt));
        if (userId is <= 0 || verifiedOrderId == Guid.Empty || !Enum.IsDefined(subject))
        {
            throw new DomainException("Contact message identity or subject is invalid.");
        }

        ReferenceNumber = NormalizeRequired(referenceNumber, MaximumReferenceNumberLength, 8, "Reference number");
        UserId = userId;
        Name = NormalizeRequired(name, MaximumNameLength, 2, "Name");
        Email = NormalizeEmail(email);
        Phone = NormalizeOptional(phone, MaximumPhoneLength, "Phone");
        Subject = subject;
        ProvidedOrderNumber = NormalizeOptional(providedOrderNumber, MaximumOrderNumberLength, "Order number");
        VerifiedOrderId = verifiedOrderId;
        Message = NormalizeRequired(message, MaximumMessageLength, 20, "Message");
        PrivacyNoticeVersion = NormalizeRequired(privacyNoticeVersion, MaximumPrivacyNoticeVersionLength, 1, "Privacy notice version");
        PrivacyNoticePublishedAt = privacyNoticePublishedAt;
        Status = ContactMessageStatus.New;
        CreatedAt = utcNow;
        ConcurrencyToken = Guid.NewGuid();
        _activities.Add(ContactMessageActivity.CreateSubmitted(this, utcNow));
    }

    // Burada yönetim durum geçişini allowlist üzerinden uygulayıp audit kaydı oluşturuyorum.
    public void ChangeStatus(ContactMessageStatus targetStatus, long adminUserId, DateTime utcNow)
    {
        EnsureAdmin(adminUserId);
        EnsureUtc(utcNow, nameof(utcNow));
        if (!Enum.IsDefined(targetStatus) || !CanTransition(Status, targetStatus))
        {
            throw new DomainException($"Contact message status cannot change from {Status} to {targetStatus}.");
        }

        var previous = Status;
        Status = targetStatus;
        ResolvedAt = targetStatus == ContactMessageStatus.Resolved ? utcNow : ResolvedAt;
        ClosedAt = targetStatus == ContactMessageStatus.Closed ? utcNow : ClosedAt;
        _activities.Add(ContactMessageActivity.CreateStatusChanged(this, adminUserId, previous, targetStatus, utcNow));
        Touch();
    }

    // Burada mesajın yönetici atamasını değiştirip eski ve yeni atamayı audit geçmişine ekliyorum.
    public void Assign(long? adminUserId, long actorAdminUserId, DateTime utcNow)
    {
        EnsureAdmin(actorAdminUserId);
        if (adminUserId is <= 0)
        {
            throw new DomainException("Assigned admin user id must be positive when supplied.");
        }

        EnsureUtc(utcNow, nameof(utcNow));
        var previous = AssignedAdminUserId;
        if (previous == adminUserId)
        {
            return;
        }

        AssignedAdminUserId = adminUserId;
        _activities.Add(ContactMessageActivity.CreateAssignmentChanged(this, actorAdminUserId, previous, adminUserId, utcNow));
        Touch();
    }

    // Burada dahili notu yalnız append-only audit etkinliği olarak kaydediyorum.
    public void AddInternalNote(string note, long adminUserId, DateTime utcNow)
    {
        EnsureAdmin(adminUserId);
        EnsureUtc(utcNow, nameof(utcNow));
        var normalizedNote = NormalizeRequired(note, MaximumNoteLength, 1, "Internal note");
        _activities.Add(ContactMessageActivity.CreateInternalNote(this, adminUserId, normalizedNote, utcNow));
        Touch();
    }

    // Burada müşteri yanıtını immutable kayıt ve audit etkinliğiyle kuyruğa hazırlıyorum.
    public ContactMessageReply QueueReply(
        string body,
        long adminUserId,
        string keyHash,
        string requestFingerprint,
        EmailOutboxMessage outboxMessage,
        DateTime utcNow)
    {
        EnsureAdmin(adminUserId);
        EnsureUtc(utcNow, nameof(utcNow));
        if (AnonymizedAt.HasValue)
        {
            throw new DomainException("An anonymized contact message cannot receive replies.");
        }

        var reply = new ContactMessageReply(
            this,
            adminUserId,
            NormalizeRequired(body, MaximumReplyLength, 1, "Reply"),
            keyHash,
            requestFingerprint,
            outboxMessage,
            utcNow);
        _replies.Add(reply);
        _activities.Add(ContactMessageActivity.CreateReplyQueued(this, adminUserId, reply.Id, utcNow));
        FirstRespondedAt ??= utcNow;
        if (Status is ContactMessageStatus.New or ContactMessageStatus.InProgress)
        {
            var previousStatus = Status;
            Status = ContactMessageStatus.WaitingForCustomer;
            _activities.Add(ContactMessageActivity.CreateStatusChanged(this, adminUserId, previousStatus, Status, utcNow));
        }

        Touch();
        return reply;
    }

    // Burada retention süresi dolan başvurunun PII içeriğini silip audit metadata'sını koruyorum.
    public void AnonymizeForRetention(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (AnonymizedAt.HasValue)
        {
            return;
        }

        UserId = null;
        Name = "Anonymized";
        Email = $"anonymized-{Id:N}@invalid.local";
        Phone = null;
        ProvidedOrderNumber = null;
        VerifiedOrderId = null;
        Message = "[Anonymized by retention policy]";
        foreach (var activity in _activities)
        {
            activity.RedactPersonalContentForRetention();
        }

        foreach (var reply in _replies)
        {
            reply.RedactBodyForRetention();
        }

        AnonymizedAt = utcNow;
        Touch();
    }

    // Burada istemcinin gönderdiği concurrency tokenın aggregate'ın güncel sürümüyle eşleşip eşleşmediğini denetliyorum.
    public bool HasConcurrencyToken(Guid expectedToken) => expectedToken != Guid.Empty && expectedToken == ConcurrencyToken;

    // Burada iletişim durumu için açık geçiş matrisini tanımlıyorum.
    private static bool CanTransition(ContactMessageStatus current, ContactMessageStatus target) =>
        current != target && current switch
        {
            ContactMessageStatus.New => target is ContactMessageStatus.InProgress or ContactMessageStatus.WaitingForCustomer or ContactMessageStatus.Closed or ContactMessageStatus.Spam,
            ContactMessageStatus.InProgress => target is ContactMessageStatus.WaitingForCustomer or ContactMessageStatus.Resolved or ContactMessageStatus.Closed or ContactMessageStatus.Spam,
            ContactMessageStatus.WaitingForCustomer => target is ContactMessageStatus.InProgress or ContactMessageStatus.Resolved or ContactMessageStatus.Closed or ContactMessageStatus.Spam,
            ContactMessageStatus.Resolved => target is ContactMessageStatus.InProgress or ContactMessageStatus.Closed,
            ContactMessageStatus.Closed => target == ContactMessageStatus.InProgress,
            ContactMessageStatus.Spam => target is ContactMessageStatus.New or ContactMessageStatus.Closed,
            _ => false
        };

    // Burada aggregate güncelleme zamanını ve optimistic concurrency tokenını birlikte yeniliyorum.
    private void Touch()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }

    // Burada yönetici kimliğinin domain içinde pozitif olmasını doğruluyorum.
    private static void EnsureAdmin(long adminUserId)
    {
        if (adminUserId <= 0)
        {
            throw new DomainException("Admin user id must be positive.");
        }
    }

    // Burada zaman değerinin UTC olduğunu doğruluyorum.
    private static void EnsureUtc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }

    // Burada zorunlu düz metni kontrol karakterlerinden arındırmadan güvenli biçimde doğrulayıp trimliyorum.
    internal static string NormalizeRequired(string value, int maximumLength, int minimumLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length < minimumLength || normalized.Length > maximumLength || ContainsUnsafeText(normalized))
        {
            throw new DomainException($"{fieldName} is invalid.");
        }

        return normalized;
    }

    // Burada opsiyonel düz metni uzunluk ve kontrol karakteri kurallarıyla normalize ediyorum.
    internal static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(value, maximumLength, 1, fieldName);
    }

    // Burada e-posta adresini küçük harfe çevirip temel güvenli biçimi doğruluyorum.
    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, MaximumEmailLength, 5, "Email").ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal) || normalized.Any(char.IsWhiteSpace))
        {
            throw new DomainException("Email is invalid.");
        }

        return normalized;
    }

    // Burada NUL, HTML ve izin verilmeyen kontrol karakterlerini reddediyorum.
    public static bool ContainsUnsafeText(string value) =>
        value.Contains('\0') || value.Contains('<') || value.Contains('>') ||
        value.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t');
}
