using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Contacts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ContactMessageApplicationTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    // Burada yeni submission'ın ContactMessage, idempotency ve outbox kayıtlarını aynı transaction akışında oluşturduğunu doğruluyorum.
    [Fact]
    public async Task Submit_Should_Create_Contact_Idempotency_And_Outbox()
    {
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.ReferenceNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var outbox = new Mock<IEmailOutboxRepository>();
        var protection = new Mock<IContactProtectionService>();
        var unitOfWork = CreateUnitOfWork<ContactSubmissionReceiptDto>();
        ContactMessage? created = null;
        contacts.Setup(repository => repository.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMessage, CancellationToken>((message, _) => created = message)
            .Returns(Task.CompletedTask);
        var handler = CreateSubmitHandler(contacts, outbox, protection, unitOfWork);

        var receipt = await handler.Handle(CreateSubmitCommand(), CancellationToken.None);

        receipt.ReferenceNumber.Should().StartWith("CNT-");
        created.Should().NotBeNull();
        created!.VerifiedOrderId.Should().BeNull();
        contacts.Verify(repository => repository.AddSubmissionIdempotencyAsync(It.IsAny<ContactSubmissionIdempotency>(), It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(repository => repository.AddAsync(It.Is<EmailOutboxMessage>(message => message.Type == EmailOutboxMessageType.ContactMessageReceived), It.IsAny<CancellationToken>()), Times.Once);
        protection.Verify(service => service.EvaluateAsync(It.IsAny<ContactProtectionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aynı key ve aynı canonical body tekrarında önceki receipt'in dönüp yeni side effect oluşmadığını doğruluyorum.
    [Fact]
    public async Task Submit_Should_Replay_Same_Key_And_Body()
    {
        var command = CreateSubmitCommand();
        var existingMessage = CreateExistingMessage();
        var existing = new ContactSubmissionIdempotency(
            Hash(command.IdempotencyKey),
            CreateFingerprint(command),
            existingMessage,
            UtcNow.AddHours(24));
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetSubmissionIdempotencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var outbox = new Mock<IEmailOutboxRepository>();
        var protection = new Mock<IContactProtectionService>();
        var handler = CreateSubmitHandler(contacts, outbox, protection, CreateUnitOfWork<ContactSubmissionReceiptDto>());

        var receipt = await handler.Handle(command, CancellationToken.None);

        receipt.ReferenceNumber.Should().Be(existingMessage.ReferenceNumber);
        protection.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
    }

    // Burada aynı key farklı body ile kullanıldığında güvenli 409 sözleşmesi üretildiğini doğruluyorum.
    [Fact]
    public async Task Submit_Should_Reject_Reused_Key_With_Different_Body()
    {
        var command = CreateSubmitCommand();
        var existingMessage = CreateExistingMessage();
        var existing = new ContactSubmissionIdempotency(
            Hash(command.IdempotencyKey),
            new string('A', 64),
            existingMessage,
            UtcNow.AddHours(24));
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetSubmissionIdempotencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var handler = CreateSubmitHandler(contacts, new Mock<IEmailOutboxRepository>(), new Mock<IContactProtectionService>(), CreateUnitOfWork<ContactSubmissionReceiptDto>());

        var action = () => handler.Handle(command, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.ErrorCode.Should().Be("idempotency_key_reused");
    }

    // Burada anonim gönderimde verilen sipariş numarasının doğrulanmış order bağına dönüşmediğini doğruluyorum.
    [Fact]
    public async Task Submit_Should_Not_Verify_Anonymous_Order_Number()
    {
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.ReferenceNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ContactMessage? created = null;
        contacts.Setup(repository => repository.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMessage, CancellationToken>((message, _) => created = message)
            .Returns(Task.CompletedTask);
        var orders = new Mock<IOrderRepository>();
        var handler = CreateSubmitHandler(
            contacts,
            new Mock<IEmailOutboxRepository>(),
            new Mock<IContactProtectionService>(),
            CreateUnitOfWork<ContactSubmissionReceiptDto>(),
            orders);

        await handler.Handle(CreateSubmitCommand(), CancellationToken.None);

        created!.ProvidedOrderNumber.Should().Be("ORD-UNTRUSTED");
        created.VerifiedOrderId.Should().BeNull();
        orders.Verify(repository => repository.GetByOrderNumberForUserAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada status mutation'ın beklenen tokenı denetleyip audit ile tek transaction'da kaydedildiğini doğruluyorum.
    [Fact]
    public async Task ChangeStatus_Should_Save_Audited_Mutation()
    {
        var message = CreateExistingMessage();
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetByIdForUpdateAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var unitOfWork = CreateUnitOfWork<ContactMessageDetailDto>();
        var handler = new ChangeContactMessageStatusCommandHandler(
            contacts.Object,
            CreateAdminCurrentUser(),
            CreateClock(),
            unitOfWork.Object);

        var detail = await handler.Handle(
            new ChangeContactMessageStatusCommand(message.Id, ContactMessageStatus.InProgress, message.ConcurrencyToken),
            CancellationToken.None);

        detail.Status.Should().Be(ContactMessageStatus.InProgress);
        detail.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.StatusChanged);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada atama kaldırma mutation'ının raw admin kimliği açmadan concurrency korumasıyla kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Assignment_Should_Save_Unassignment()
    {
        var message = CreateExistingMessage();
        message.Assign(99, 42, UtcNow.AddMinutes(1));
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetByIdForUpdateAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var unitOfWork = CreateUnitOfWork<ContactMessageDetailDto>();
        var handler = new AssignContactMessageCommandHandler(
            contacts.Object,
            new Mock<IUserRepository>().Object,
            CreateAdminCurrentUser(),
            CreateClock(UtcNow.AddMinutes(2)),
            unitOfWork.Object);

        var detail = await handler.Handle(
            new AssignContactMessageCommand(message.Id, null, message.ConcurrencyToken),
            CancellationToken.None);

        detail.AssignedAdminUserId.Should().BeNull();
        detail.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.AssignmentChanged);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada dahili notun append-only activity olarak kaydedilip reply koleksiyonuna karışmadığını doğruluyorum.
    [Fact]
    public async Task AddNote_Should_Create_Only_Internal_Activity()
    {
        var message = CreateExistingMessage();
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetByIdForUpdateAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var unitOfWork = CreateUnitOfWork<ContactMessageDetailDto>();
        var handler = new AddContactMessageNoteCommandHandler(contacts.Object, CreateAdminCurrentUser(), CreateClock(), unitOfWork.Object);

        var detail = await handler.Handle(
            new AddContactMessageNoteCommand(message.Id, "Yalnız yönetici tarafından görülen not.", message.ConcurrencyToken),
            CancellationToken.None);

        detail.Activities.Should().Contain(activity => activity.Type == ContactMessageActivityType.InternalNoteAdded);
        detail.Replies.Should().BeEmpty();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aynı reply intent'inin tek reply ve deterministic tek outbox oluşturduğunu doğruluyorum.
    [Fact]
    public async Task Reply_Should_Create_Outbox_Once_And_Replay_Idempotently()
    {
        var message = CreateExistingMessage();
        var contacts = new Mock<IContactMessageRepository>();
        contacts.Setup(repository => repository.GetByIdForUpdateAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var outbox = new Mock<IEmailOutboxRepository>();
        outbox.Setup(repository => repository.AddAsync(It.IsAny<EmailOutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var unitOfWork = CreateUnitOfWork<ContactMessageDetailDto>();
        var handler = new ReplyContactMessageCommandHandler(
            contacts.Object,
            outbox.Object,
            CreateAdminCurrentUser(),
            CreateClock(),
            unitOfWork.Object);
        var command = new ReplyContactMessageCommand(message.Id, "Talebinizi inceleyip size bilgi veriyoruz.", "reply-intent-1");

        await handler.Handle(command, CancellationToken.None);
        var replay = await handler.Handle(command, CancellationToken.None);

        replay.Replies.Should().ContainSingle();
        outbox.Verify(repository => repository.AddAsync(
            It.Is<EmailOutboxMessage>(item =>
                item.Type == EmailOutboxMessageType.ContactMessageReply &&
                item.DeduplicationKey == $"contact-reply:{replay.Replies[0].Id:N}"),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada submission handler'ını güvenli test seçenekleri ve sabit saatle hazırlıyorum.
    private static SubmitContactMessageCommandHandler CreateSubmitHandler(
        Mock<IContactMessageRepository> contacts,
        Mock<IEmailOutboxRepository> outbox,
        Mock<IContactProtectionService> protection,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IOrderRepository>? orders = null)
    {
        outbox.Setup(repository => repository.AddAsync(It.IsAny<EmailOutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        protection.Setup(service => service.EvaluateAsync(It.IsAny<ContactProtectionRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        contacts.Setup(repository => repository.AddSubmissionIdempotencyAsync(It.IsAny<ContactSubmissionIdempotency>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return new SubmitContactMessageCommandHandler(
            contacts.Object,
            outbox.Object,
            (orders ?? new Mock<IOrderRepository>()).Object,
            Mock.Of<ICurrentUserService>(service => service.UserId == null),
            protection.Object,
            Mock.Of<IDateTimeProvider>(provider => provider.UtcNow == UtcNow),
            unitOfWork.Object,
            new ContactPrivacyOptions
            {
                NoticeVersion = "2026-08",
                NoticePublishedAtUtc = new DateTimeOffset(UtcNow.AddDays(-10)),
                CleanupBatchSize = 100
            },
            new ContactEmailOptions
            {
                ContactInboxAddress = "support@example.com",
                AdminContactMessageBaseUrl = "https://admin.example.com/contact-messages"
            });
    }

    // Burada test submission komutunu aynı canonical değerlerle hazırlıyorum.
    private static SubmitContactMessageCommand CreateSubmitCommand() =>
        new(
            "Ada Lovelace",
            "ada@example.com",
            null,
            ContactMessageSubject.OrderSupport,
            "ORD-UNTRUSTED",
            "Siparişim hakkında ayrıntılı destek rica ediyorum.",
            "contact-test-key",
            "turnstile-token",
            null);

    // Burada replay testleri için kalıcı receipt sahibi contact aggregate'ını hazırlıyorum.
    private static ContactMessage CreateExistingMessage() =>
        new(
            "CNT-0123456789ABCDEF0123",
            null,
            "Ada Lovelace",
            "ada@example.com",
            null,
            ContactMessageSubject.OrderSupport,
            "ORD-UNTRUSTED",
            null,
            "Siparişim hakkında ayrıntılı destek rica ediyorum.",
            "2026-08",
            UtcNow.AddDays(-10),
            UtcNow);

    // Burada admin mutation testleri için kimliği sabit current user hazırlıyorum.
    private static ICurrentUserService CreateAdminCurrentUser() =>
        Mock.Of<ICurrentUserService>(service => service.UserId == 42);

    // Burada mutation testleri için istenen UTC anını döndüren saat hazırlıyorum.
    private static IDateTimeProvider CreateClock(DateTime? utcNow = null) =>
        Mock.Of<IDateTimeProvider>(provider => provider.UtcNow == (utcNow ?? UtcNow.AddMinutes(5)));

    // Burada test idempotency anahtarını production ile aynı SHA-256 biçimine çeviriyorum.
    private static string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    // Burada test canonical fingerprint değerini production uzunluk-ayrımlı kuralla üretiyorum.
    private static string CreateFingerprint(SubmitContactMessageCommand command)
    {
        var fields = new[] { command.Name.Trim(), command.Email.Trim().ToLowerInvariant(), string.Empty, "0", command.OrderNumber!, command.Message.Trim() };
        return Hash(string.Join('|', fields.Select(value => $"{value.Length}:{value}")));
    }

    // Burada serializable transaction delegesini doğrudan çalıştıran mock unit of work hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork<T>()
    {
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(item => item.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task<T>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<T>>, CancellationToken>((operation, token) => operation(token));
        unit.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unit;
    }
}
