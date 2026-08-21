using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Contacts;

public sealed record SubmitContactMessageCommand(
    string Name,
    string Email,
    string? Phone,
    ContactMessageSubject Subject,
    string? OrderNumber,
    string Message,
    string IdempotencyKey,
    string? TurnstileToken,
    string? ClientIpAddress) : IRequest<ContactSubmissionReceiptDto>;

public sealed record GetContactMessagesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    ContactMessageStatus? Status = null,
    ContactMessageSubject? Subject = null,
    string? AssignedAdminUserId = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<ContactMessageSummaryDto>>;

public sealed record GetContactMessageByIdQuery(Guid Id) : IRequest<ContactMessageDetailDto>;
public sealed record ChangeContactMessageStatusCommand(Guid Id, ContactMessageStatus Status, Guid ExpectedConcurrencyToken) : IRequest<ContactMessageDetailDto>;
public sealed record AssignContactMessageCommand(Guid Id, string? AssignedAdminUserId, Guid ExpectedConcurrencyToken) : IRequest<ContactMessageDetailDto>;
public sealed record AddContactMessageNoteCommand(Guid Id, string Note, Guid ExpectedConcurrencyToken) : IRequest<ContactMessageDetailDto>;
public sealed record ReplyContactMessageCommand(Guid Id, string Body, string IdempotencyKey) : IRequest<ContactMessageDetailDto>;

public sealed class SubmitContactMessageCommandValidator : AbstractValidator<SubmitContactMessageCommand>
{
    // Burada public iletişim request alanlarını uzunluk, enum, e-posta ve düz metin kurallarıyla doğruluyorum.
    public SubmitContactMessageCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MinimumLength(2).MaximumLength(ContactMessage.MaximumNameLength).Must(BeSafePlainText);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(ContactMessage.MaximumEmailLength).EmailAddress().Must(BeSafePlainText);
        RuleFor(command => command.Phone).MaximumLength(ContactMessage.MaximumPhoneLength).Must(BeSafeOptionalPlainText);
        RuleFor(command => command.OrderNumber).MaximumLength(ContactMessage.MaximumOrderNumberLength).Must(BeSafeOptionalPlainText);
        RuleFor(command => command.Message).NotEmpty().MinimumLength(20).MaximumLength(ContactMessage.MaximumMessageLength).Must(BeSafePlainText);
        RuleFor(command => command.Subject).Must(value => Enum.IsDefined(value));
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200).Must(BeSafeHeaderValue);
        RuleFor(command => command.TurnstileToken).MaximumLength(2048).Must(BeSafeHeaderValue);
    }

    // Burada zorunlu kullanıcı metninde HTML ve tehlikeli kontrol karakterlerini reddediyorum.
    private static bool BeSafePlainText(string value) => !string.IsNullOrEmpty(value) && !ContactMessage.ContainsUnsafeText(value);

    // Burada opsiyonel kullanıcı metninde HTML ve tehlikeli kontrol karakterlerini reddediyorum.
    private static bool BeSafeOptionalPlainText(string? value) => string.IsNullOrEmpty(value) || !ContactMessage.ContainsUnsafeText(value);

    // Burada header değerinde CRLF, NUL ve izin verilmeyen kontrol karakterlerini reddediyorum.
    private static bool BeSafeHeaderValue(string? value) => string.IsNullOrEmpty(value) || !value.Any(char.IsControl);
}

public sealed class GetContactMessagesQueryValidator : AbstractValidator<GetContactMessagesQuery>
{
    // Burada admin liste parametrelerini bounded pagination ve geçerli filtrelerle doğruluyorum.
    public GetContactMessagesQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.Status).Must(value => !value.HasValue || Enum.IsDefined(value.Value));
        RuleFor(query => query.Subject).Must(value => !value.HasValue || Enum.IsDefined(value.Value));
        RuleFor(query => query.CreatedFromUtc).Must(BeUtcWhenProvided).WithMessage("Created from must use UTC.");
        RuleFor(query => query.CreatedToUtc).Must(BeUtcWhenProvided).WithMessage("Created to must use UTC.");
        RuleFor(query => query).Must(query => !query.CreatedFromUtc.HasValue || !query.CreatedToUtc.HasValue || query.CreatedFromUtc <= query.CreatedToUtc)
            .WithMessage("Created from must not be after created to.");
    }

    // Burada opsiyonel tarih filtresinin yalnız UTC olarak kullanılmasını doğruluyorum.
    private static bool BeUtcWhenProvided(DateTime? value) => !value.HasValue || value.Value.Kind == DateTimeKind.Utc;
}

public sealed class ChangeContactMessageStatusCommandValidator : AbstractValidator<ChangeContactMessageStatusCommand>
{
    // Burada status mutation kimlik, enum ve concurrency alanlarını doğruluyorum.
    public ChangeContactMessageStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Status).Must(value => Enum.IsDefined(value));
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }
}

public sealed class AssignContactMessageCommandValidator : AbstractValidator<AssignContactMessageCommand>
{
    // Burada assignment mutation kimlik ve public admin kimliği alanlarını doğruluyorum.
    public AssignContactMessageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
        RuleFor(command => command.AssignedAdminUserId)
            .Must(value => value is null || PublicIdCodec.TryDecodeUserId(value, out _))
            .WithMessage("Assigned admin user id is invalid.");
    }
}

public sealed class AddContactMessageNoteCommandValidator : AbstractValidator<AddContactMessageNoteCommand>
{
    // Burada dahili not mutation alanlarını append-only sınırlarıyla doğruluyorum.
    public AddContactMessageNoteCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
        RuleFor(command => command.Note).NotEmpty().MaximumLength(ContactMessage.MaximumNoteLength)
            .Must(value => !ContactMessage.ContainsUnsafeText(value));
    }
}

public sealed class ReplyContactMessageCommandValidator : AbstractValidator<ReplyContactMessageCommand>
{
    // Burada müşteri reply intent alanlarını düz metin ve idempotency sınırlarıyla doğruluyorum.
    public ReplyContactMessageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Body).NotEmpty().MaximumLength(ContactMessage.MaximumReplyLength)
            .Must(value => !ContactMessage.ContainsUnsafeText(value));
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200)
            .Must(value => !value.Any(char.IsControl));
    }
}

public sealed class SubmitContactMessageCommandHandler : IRequestHandler<SubmitContactMessageCommand, ContactSubmissionReceiptDto>
{
    private readonly IContactMessageRepository _contacts;
    private readonly IEmailOutboxRepository _outbox;
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly IContactProtectionService _protection;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ContactPrivacyOptions _privacy;
    private readonly ContactEmailOptions _email;

    // Burada public submission akışının persistence, koruma, kullanıcı ve yapılandırma bağımlılıklarını hazırlıyorum.
    public SubmitContactMessageCommandHandler(
        IContactMessageRepository contacts,
        IEmailOutboxRepository outbox,
        IOrderRepository orders,
        ICurrentUserService currentUser,
        IContactProtectionService protection,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        ContactPrivacyOptions privacy,
        ContactEmailOptions email)
    {
        _contacts = contacts;
        _outbox = outbox;
        _orders = orders;
        _currentUser = currentUser;
        _protection = protection;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _privacy = privacy;
        _email = email;
    }

    // Burada replay kontrolünü SMTP dışı korumadan önce yapıp yeni intent'i serializable transaction'a taşıyorum.
    public async Task<ContactSubmissionReceiptDto> Handle(SubmitContactMessageCommand request, CancellationToken cancellationToken)
    {
        var keyHash = ContactHashing.Sha256(request.IdempotencyKey.Trim());
        var fingerprint = ContactHashing.CreateSubmissionFingerprint(request);
        var existing = await _contacts.GetSubmissionIdempotencyAsync(keyHash, cancellationToken);
        if (existing is not null)
        {
            return ResolveReplay(existing, fingerprint);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        await _protection.EvaluateAsync(
            new ContactProtectionRequest(normalizedEmail, request.ClientIpAddress, request.TurnstileToken),
            cancellationToken);

        return await _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => SubmitInTransactionAsync(request, keyHash, fingerprint, token),
            cancellationToken);
    }

    // Burada idempotency tekrarını yeniden denetleyip ContactMessage ve outbox kaydını atomik oluşturuyorum.
    private async Task<ContactSubmissionReceiptDto> SubmitInTransactionAsync(
        SubmitContactMessageCommand request,
        string keyHash,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await _contacts.GetSubmissionIdempotencyForUpdateAsync(keyHash, cancellationToken);
        if (existing is not null)
        {
            return ResolveReplay(existing, fingerprint);
        }

        var utcNow = _clock.UtcNow;
        Guid? verifiedOrderId = null;
        if (_currentUser.UserId is { } userId && !string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            var ownedOrder = await _orders.GetByOrderNumberForUserAsync(request.OrderNumber.Trim(), userId, cancellationToken);
            verifiedOrderId = ownedOrder?.Id;
        }

        var contact = new ContactMessage(
            await CreateUniqueReferenceNumberAsync(cancellationToken),
            _currentUser.UserId,
            request.Name,
            request.Email,
            request.Phone,
            request.Subject,
            request.OrderNumber,
            verifiedOrderId,
            request.Message,
            _privacy.NoticeVersion,
            _privacy.NoticePublishedAtUtc.UtcDateTime,
            utcNow);
        var outbox = EmailOutboxMessage.CreateContactMessageReceived(
            _email.ContactInboxAddress,
            contact.Id,
            utcNow);
        var idempotency = new ContactSubmissionIdempotency(
            keyHash,
            fingerprint,
            contact,
            utcNow.AddHours(24));
        await _contacts.AddAsync(contact, cancellationToken);
        await _contacts.AddSubmissionIdempotencyAsync(idempotency, cancellationToken);
        await _outbox.AddAsync(outbox, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ContactSubmissionReceiptDto(contact.ReferenceNumber, contact.CreatedAt);
    }

    // Burada aynı anahtarın yalnız aynı canonical body için önceki receipt sonucunu döndürmesini sağlıyorum.
    private static ContactSubmissionReceiptDto ResolveReplay(ContactSubmissionIdempotency existing, string fingerprint)
    {
        if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new ApiContractException(409, "idempotency_key_reused", "Idempotency key reused", "Idempotency key was already used for a different contact submission.");
        }

        return new ContactSubmissionReceiptDto(existing.ReferenceNumber, existing.SubmittedAt);
    }

    // Burada tahmin edilmesi zor ve benzersiz public reference numarasını sınırlı denemeyle üretiyorum.
    private async Task<string> CreateUniqueReferenceNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var value = $"CNT-{Convert.ToHexString(RandomNumberGenerator.GetBytes(10))}";
            if (!await _contacts.ReferenceNumberExistsAsync(value, cancellationToken))
            {
                return value;
            }
        }

        throw new ConflictException("A unique contact reference number could not be generated.");
    }
}

public sealed class GetContactMessagesQueryHandler : IRequestHandler<GetContactMessagesQuery, PagedResult<ContactMessageSummaryDto>>
{
    private readonly IContactMessageRepository _contacts;

    // Burada admin iletişim listesi reader bağımlılığını hazırlıyorum.
    public GetContactMessagesQueryHandler(IContactMessageRepository contacts) => _contacts = contacts;

    // Burada public admin kimliği filtresini iç kimliğe çevirip sayfalı özetleri getiriyorum.
    public async Task<PagedResult<ContactMessageSummaryDto>> Handle(GetContactMessagesQuery request, CancellationToken cancellationToken)
    {
        long? assigneeId = request.AssignedAdminUserId is null
            ? null
            : PublicIdCodec.DecodeUserId(request.AssignedAdminUserId);
        var result = await _contacts.GetListAsync(
            new ContactMessageListFilter(
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.Status,
                request.Subject,
                assigneeId,
                request.CreatedFromUtc,
                request.CreatedToUtc),
            cancellationToken);
        return result.Map(ContactDtoMapping.ToSummaryDto);
    }
}

public sealed class GetContactMessageByIdQueryHandler : IRequestHandler<GetContactMessageByIdQuery, ContactMessageDetailDto>
{
    private readonly IContactMessageRepository _contacts;

    // Burada admin iletişim detay repository bağımlılığını hazırlıyorum.
    public GetContactMessageByIdQueryHandler(IContactMessageRepository contacts) => _contacts = contacts;

    // Burada tam audit grafiğini admin detay DTO'su olarak getiriyorum.
    public async Task<ContactMessageDetailDto> Handle(GetContactMessageByIdQuery request, CancellationToken cancellationToken) =>
        (await _contacts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Contact message was not found.")).ToDetailDto();
}

public abstract class ContactMutationHandlerBase
{
    protected readonly IContactMessageRepository Contacts;
    protected readonly ICurrentUserService CurrentUser;
    protected readonly IDateTimeProvider Clock;
    protected readonly IUnitOfWork UnitOfWork;

    // Burada ortak admin mutation bağımlılıklarını hazırlıyorum.
    protected ContactMutationHandlerBase(IContactMessageRepository contacts, ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        Contacts = contacts;
        CurrentUser = currentUser;
        Clock = clock;
        UnitOfWork = unitOfWork;
    }

    // Burada güncel aggregate'ı kilitli getirip stale token kullanımını 409 ile reddediyorum.
    protected async Task<ContactMessage> GetForMutationAsync(Guid id, Guid expectedToken, CancellationToken cancellationToken)
    {
        var contact = await Contacts.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Contact message was not found.");
        if (!contact.HasConcurrencyToken(expectedToken))
        {
            throw new ConcurrencyException("Contact message was changed by another operation.");
        }

        return contact;
    }
}

public sealed class ChangeContactMessageStatusCommandHandler : ContactMutationHandlerBase, IRequestHandler<ChangeContactMessageStatusCommand, ContactMessageDetailDto>
{
    // Burada status mutation bağımlılıklarını ortak taban üzerinden hazırlıyorum.
    public ChangeContactMessageStatusCommandHandler(IContactMessageRepository contacts, ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
        : base(contacts, currentUser, clock, unitOfWork) { }

    // Burada status değişimini serializable transaction içinde audit geçmişiyle kaydediyorum.
    public Task<ContactMessageDetailDto> Handle(ChangeContactMessageStatusCommand request, CancellationToken cancellationToken) =>
        UnitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var contact = await GetForMutationAsync(request.Id, request.ExpectedConcurrencyToken, token);
            contact.ChangeStatus(request.Status, CurrentUser.GetRequiredUserId(), Clock.UtcNow);
            await UnitOfWork.SaveChangesAsync(token);
            return contact.ToDetailDto();
        }, cancellationToken);
}

public sealed class AssignContactMessageCommandHandler : ContactMutationHandlerBase, IRequestHandler<AssignContactMessageCommand, ContactMessageDetailDto>
{
    private readonly IUserRepository _users;

    // Burada assignment mutation için kullanıcı repository bağımlılığını hazırlıyorum.
    public AssignContactMessageCommandHandler(IContactMessageRepository contacts, IUserRepository users, ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
        : base(contacts, currentUser, clock, unitOfWork) => _users = users;

    // Burada yalnız aktif admin kullanıcıya atamayı concurrency korumasıyla kaydediyorum.
    public Task<ContactMessageDetailDto> Handle(AssignContactMessageCommand request, CancellationToken cancellationToken) =>
        UnitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var contact = await GetForMutationAsync(request.Id, request.ExpectedConcurrencyToken, token);
            long? assigneeId = request.AssignedAdminUserId is null ? null : PublicIdCodec.DecodeUserId(request.AssignedAdminUserId);
            if (assigneeId.HasValue)
            {
                var user = await _users.GetByIdAsync(assigneeId.Value, token);
                if (user is null || user.Role != UserRole.Admin || user.Status != UserStatus.Active)
                {
                    throw new ConflictException("Assigned user must be an active administrator.");
                }
            }

            contact.Assign(assigneeId, CurrentUser.GetRequiredUserId(), Clock.UtcNow);
            await UnitOfWork.SaveChangesAsync(token);
            return contact.ToDetailDto();
        }, cancellationToken);
}

public sealed class AddContactMessageNoteCommandHandler : ContactMutationHandlerBase, IRequestHandler<AddContactMessageNoteCommand, ContactMessageDetailDto>
{
    // Burada note mutation bağımlılıklarını ortak taban üzerinden hazırlıyorum.
    public AddContactMessageNoteCommandHandler(IContactMessageRepository contacts, ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
        : base(contacts, currentUser, clock, unitOfWork) { }

    // Burada dahili notu append-only activity olarak transaction içinde kaydediyorum.
    public Task<ContactMessageDetailDto> Handle(AddContactMessageNoteCommand request, CancellationToken cancellationToken) =>
        UnitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var contact = await GetForMutationAsync(request.Id, request.ExpectedConcurrencyToken, token);
            contact.AddInternalNote(request.Note, CurrentUser.GetRequiredUserId(), Clock.UtcNow);
            await UnitOfWork.SaveChangesAsync(token);
            return contact.ToDetailDto();
        }, cancellationToken);
}

public sealed class ReplyContactMessageCommandHandler : IRequestHandler<ReplyContactMessageCommand, ContactMessageDetailDto>
{
    private readonly IContactMessageRepository _contacts;
    private readonly IEmailOutboxRepository _outbox;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada reply ve outbox atomicity bağımlılıklarını hazırlıyorum.
    public ReplyContactMessageCommandHandler(IContactMessageRepository contacts, IEmailOutboxRepository outbox, ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _contacts = contacts;
        _outbox = outbox;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada aynı reply intent'ini tek outbox kaydına bağlayarak serializable transaction içinde kuyruğa alıyorum.
    public Task<ContactMessageDetailDto> Handle(ReplyContactMessageCommand request, CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var keyHash = ContactHashing.Sha256(request.IdempotencyKey.Trim());
            var fingerprint = ContactHashing.Sha256($"{request.Id:N}|{request.Body.Trim()}");
            var contact = await _contacts.GetByIdForUpdateAsync(request.Id, token)
                ?? throw new NotFoundException("Contact message was not found.");
            var existing = contact.Replies.FirstOrDefault(reply => reply.IdempotencyKeyHash == keyHash);
            if (existing is not null)
            {
                if (existing.RequestFingerprint != fingerprint)
                {
                    throw new ApiContractException(409, "idempotency_key_reused", "Idempotency key reused", "Idempotency key was already used for a different reply.");
                }

                return contact.ToDetailDto();
            }

            var utcNow = _clock.UtcNow;
            var outbox = EmailOutboxMessage.CreateContactMessageReply(contact.Email, contact.Id, utcNow);
            var reply = contact.QueueReply(request.Body, _currentUser.GetRequiredUserId(), keyHash, fingerprint, outbox, utcNow);
            outbox.LinkContactReply(reply.Id);
            await _outbox.AddAsync(outbox, token);
            await _unitOfWork.SaveChangesAsync(token);
            return contact.ToDetailDto();
        }, cancellationToken);
}

internal static class ContactHashing
{
    // Burada hassas anahtar ve canonical intent değerlerini tek yönlü SHA-256 hex değerine çeviriyorum.
    internal static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    // Burada public submission body alanlarını uzunluk-ayrımlı canonical fingerprint değerine çeviriyorum.
    internal static string CreateSubmissionFingerprint(SubmitContactMessageCommand request)
    {
        var fields = new[]
        {
            request.Name.Trim(),
            request.Email.Trim().ToLowerInvariant(),
            request.Phone?.Trim() ?? string.Empty,
            ((int)request.Subject).ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.OrderNumber?.Trim() ?? string.Empty,
            request.Message.Trim()
        };
        var canonical = string.Join('|', fields.Select(value => $"{value.Length}:{value}"));
        return Sha256(canonical);
    }
}
