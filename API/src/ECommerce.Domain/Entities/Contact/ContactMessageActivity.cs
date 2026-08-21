using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class ContactMessageActivity : BaseEntity
{
    public Guid ContactMessageId { get; private set; }
    public ContactMessage ContactMessage { get; private set; } = null!;
    public ContactMessageActivityType Type { get; private set; }
    public long? ActorAdminUserId { get; private set; }
    public User? ActorAdminUser { get; private set; }
    public string? Content { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid? ReplyId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un immutable activity kaydını yükleyebilmesi için boş kurucuyu tutuyorum.
    private ContactMessageActivity()
    {
    }

    // Burada immutable activity kaydını yalnız aggregate factory metotları için oluşturuyorum.
    private ContactMessageActivity(ContactMessage message, ContactMessageActivityType type, long? actorAdminUserId, DateTime utcNow)
    {
        ContactMessageId = message.Id;
        ContactMessage = message;
        Type = type;
        ActorAdminUserId = actorAdminUserId;
        CreatedAt = utcNow;
    }

    // Burada başvurunun ilk oluşturulma audit kaydını hazırlıyorum.
    internal static ContactMessageActivity CreateSubmitted(ContactMessage message, DateTime utcNow) =>
        new(message, ContactMessageActivityType.Submitted, null, utcNow);

    // Burada durum değişiminin önceki ve yeni değerlerini immutable audit kaydına yazıyorum.
    internal static ContactMessageActivity CreateStatusChanged(ContactMessage message, long actorId, ContactMessageStatus previous, ContactMessageStatus current, DateTime utcNow) =>
        new(message, ContactMessageActivityType.StatusChanged, actorId, utcNow)
        {
            PreviousValue = previous.ToString(),
            NewValue = current.ToString()
        };

    // Burada atama değişiminin public API'ye açılmayacak iç kimliklerini audit kaydına yazıyorum.
    internal static ContactMessageActivity CreateAssignmentChanged(ContactMessage message, long actorId, long? previous, long? current, DateTime utcNow) =>
        new(message, ContactMessageActivityType.AssignmentChanged, actorId, utcNow)
        {
            PreviousValue = previous?.ToString(),
            NewValue = current?.ToString()
        };

    // Burada dahili notu ayrı ve append-only bir activity kaydı olarak oluşturuyorum.
    internal static ContactMessageActivity CreateInternalNote(ContactMessage message, long actorId, string note, DateTime utcNow) =>
        new(message, ContactMessageActivityType.InternalNoteAdded, actorId, utcNow) { Content = note };

    // Burada kuyruğa alınan yanıtı ilgili reply kimliğiyle audit geçmişine bağlıyorum.
    internal static ContactMessageActivity CreateReplyQueued(ContactMessage message, long actorId, Guid replyId, DateTime utcNow) =>
        new(message, ContactMessageActivityType.ReplyQueued, actorId, utcNow) { ReplyId = replyId };

    // Burada audit tipi, aktörü ve zamanını korurken dahili not içeriğini retention kapsamında siliyorum.
    internal void RedactPersonalContentForRetention() => Content = null;
}
