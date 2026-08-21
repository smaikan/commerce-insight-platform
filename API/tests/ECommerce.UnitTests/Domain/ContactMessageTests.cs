using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Common;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ContactMessageTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    // Burada public başvurunun normalizasyon, başlangıç durumu ve immutable submitted activity kurallarını doğruluyorum.
    [Fact]
    public void Constructor_Should_Normalize_And_Create_Submitted_Activity()
    {
        var message = CreateMessage();

        message.Name.Should().Be("Ada Lovelace");
        message.Email.Should().Be("ada@example.com");
        message.Status.Should().Be(ContactMessageStatus.New);
        message.Activities.Should().ContainSingle(activity => activity.Type == ContactMessageActivityType.Submitted);
        message.ConcurrencyToken.Should().NotBeEmpty();
    }

    // Burada HTML ve NUL içeren kullanıcı mesajlarının domain sınırında reddedildiğini doğruluyorum.
    [Theory]
    [InlineData("<script>alert(1)</script> Bu metin yeterince uzundur")]
    [InlineData("Bu metin NUL içerir \0 ve yeterince uzundur")]
    public void Constructor_Should_Reject_Unsafe_Text(string body)
    {
        var action = () => CreateMessage(body);

        action.Should().Throw<DomainException>();
    }

    // Burada status allowlist geçişinin timestamp, token ve audit geçmişini güncellediğini doğruluyorum.
    [Fact]
    public void ChangeStatus_Should_Apply_Allowlist_And_Refresh_Concurrency()
    {
        var message = CreateMessage();
        var initialToken = message.ConcurrencyToken;

        message.ChangeStatus(ContactMessageStatus.InProgress, 42, UtcNow.AddMinutes(1));
        message.ChangeStatus(ContactMessageStatus.Resolved, 42, UtcNow.AddMinutes(2));

        message.Status.Should().Be(ContactMessageStatus.Resolved);
        message.ResolvedAt.Should().Be(UtcNow.AddMinutes(2));
        message.ConcurrencyToken.Should().NotBe(initialToken);
        message.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.StatusChanged);
    }

    // Burada geçersiz durum sıçramasının aggregate durumunu değiştirmediğini doğruluyorum.
    [Fact]
    public void ChangeStatus_Should_Reject_Invalid_Transition()
    {
        var message = CreateMessage();

        var action = () => message.ChangeStatus(ContactMessageStatus.Resolved, 42, UtcNow.AddMinutes(1));

        action.Should().Throw<DomainException>();
        message.Status.Should().Be(ContactMessageStatus.New);
    }

    // Burada dahili not ile müşteri reply kaydının ayrı immutable audit türleri ürettiğini doğruluyorum.
    [Fact]
    public void Note_And_Reply_Should_Create_Separate_Activities()
    {
        var message = CreateMessage();
        message.AddInternalNote("Müşterinin siparişini owner scope ile kontrol et.", 42, UtcNow.AddMinutes(1));
        var outbox = EmailOutboxMessage.CreateContactMessageReply(message.Email, message.Id, UtcNow.AddMinutes(2));
        var reply = message.QueueReply(
            "Talebinizi aldık ve incelemeye başladık.",
            42,
            new string('A', 64),
            new string('B', 64),
            outbox,
            UtcNow.AddMinutes(2));
        outbox.LinkContactReply(reply.Id);

        message.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.InternalNoteAdded);
        message.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.ReplyQueued && activity.ReplyId == reply.Id);
        message.Activities.Should().Contain(activity =>
            activity.Type == ContactMessageActivityType.StatusChanged &&
            activity.PreviousValue == ContactMessageStatus.New.ToString() &&
            activity.NewValue == ContactMessageStatus.WaitingForCustomer.ToString());
        message.Replies.Should().ContainSingle().Which.Body.Should().Be("Talebinizi aldık ve incelemeye başladık.");
        message.FirstRespondedAt.Should().Be(UtcNow.AddMinutes(2));
        message.Status.Should().Be(ContactMessageStatus.WaitingForCustomer);
        outbox.DeduplicationKey.Should().Be($"contact-reply:{reply.Id:N}");
    }

    // Burada retention anonimleştirmesinin PII içeriğini silerken immutable audit bağlarını koruduğunu doğruluyorum.
    [Fact]
    public void AnonymizeForRetention_Should_Redact_Pii_And_Preserve_Audit_Metadata()
    {
        var message = CreateMessage();
        message.AddInternalNote("Müşteriyle ilgili dahili not.", 42, UtcNow.AddMinutes(1));
        var outbox = EmailOutboxMessage.CreateContactMessageReply(message.Email, message.Id, UtcNow.AddMinutes(2));
        var reply = message.QueueReply(
            "Müşteriye gönderilen yanıt.",
            42,
            new string('A', 64),
            new string('B', 64),
            outbox,
            UtcNow.AddMinutes(2));
        outbox.LinkContactReply(reply.Id);
        var activityMetadata = message.Activities.Select(activity => (activity.Id, activity.Type, activity.CreatedAt)).ToList();
        var previousToken = message.ConcurrencyToken;

        message.AnonymizeForRetention(UtcNow.AddDays(61));

        message.UserId.Should().BeNull();
        message.Name.Should().Be("Anonymized");
        message.Email.Should().Be($"anonymized-{message.Id:N}@invalid.local");
        message.Phone.Should().BeNull();
        message.ProvidedOrderNumber.Should().BeNull();
        message.VerifiedOrderId.Should().BeNull();
        message.Message.Should().Be("[Anonymized by retention policy]");
        message.AnonymizedAt.Should().Be(UtcNow.AddDays(61));
        message.ConcurrencyToken.Should().NotBe(previousToken);
        message.Activities.Select(activity => (activity.Id, activity.Type, activity.CreatedAt)).Should().Equal(activityMetadata);
        message.Activities.Should().OnlyContain(activity => activity.Content == null);
        message.Replies.Should().ContainSingle().Which.Body.Should().Be("[Anonymized by retention policy]");

        var replyAction = () => message.QueueReply(
            "Retention sonrası yanıt gönderilemez.",
            42,
            new string('C', 64),
            new string('D', 64),
            EmailOutboxMessage.CreateContactMessageReply(message.Email, message.Id, UtcNow.AddDays(62)),
            UtcNow.AddDays(62));
        replyAction.Should().Throw<DomainException>();
    }

    // Burada retention işleminin bekleyen contact reply e-postasını terminalleştirip alıcı PII değerini sildiğini doğruluyorum.
    [Fact]
    public void Contact_Outbox_Retention_Should_Redact_Recipient_And_Stop_Delivery()
    {
        var message = CreateMessage();
        var outbox = EmailOutboxMessage.CreateContactMessageReply(message.Email, message.Id, UtcNow);

        outbox.AnonymizeContactDataForRetention(UtcNow.AddDays(61));

        outbox.Email.Should().Be("anonymized@invalid.local");
        outbox.DeadLetteredAt.Should().Be(UtcNow.AddDays(61));
        outbox.NextAttemptAt.Should().Be(DateTime.MaxValue);
        outbox.IsEligibleForClaim(UtcNow.AddDays(62)).Should().BeFalse();
    }

    // Burada testlerde kullanılacak geçerli iletişim aggregate'ını hazırlıyorum.
    private static ContactMessage CreateMessage(string body = "Siparişim hakkında ayrıntılı destek rica ediyorum.") =>
        new(
            "CNT-0123456789ABCDEF0123",
            7,
            "  Ada Lovelace  ",
            " ADA@EXAMPLE.COM ",
            null,
            ContactMessageSubject.OrderSupport,
            "ORD-TEST",
            Guid.NewGuid(),
            body,
            "2026-08",
            UtcNow.AddDays(-10),
            UtcNow);
}
