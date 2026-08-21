using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contacts;

public sealed record ContactMessageListFilter(
    int PageNumber,
    int PageSize,
    string? Search,
    ContactMessageStatus? Status,
    ContactMessageSubject? Subject,
    long? AssignedAdminUserId,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc);

public interface IContactMessageRepository
{
    // Burada yeni iletişim aggregate'ını aynı DbContext takibine ekliyorum.
    Task AddAsync(ContactMessage message, CancellationToken cancellationToken = default);

    // Burada hashlenmiş submission anahtarının mevcut receipt sonucunu getiriyorum.
    Task<ContactSubmissionIdempotency?> GetSubmissionIdempotencyAsync(string keyHash, CancellationToken cancellationToken = default);

    // Burada hashlenmiş anahtarı kilitli transaction içinde yeniden denetliyorum.
    Task<ContactSubmissionIdempotency?> GetSubmissionIdempotencyForUpdateAsync(string keyHash, CancellationToken cancellationToken = default);

    // Burada yeni submission idempotency sonucunu kalıcı takibe ekliyorum.
    Task AddSubmissionIdempotencyAsync(ContactSubmissionIdempotency record, CancellationToken cancellationToken = default);

    // Burada yönetim detay aggregate'ını takip etmeden getiriyorum.
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada yönetim mutasyon aggregate'ını bütün audit grafiğiyle takipli getiriyorum.
    Task<ContactMessage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada yönetim listesini sınırlı filtre ve kararlı sıralamayla getiriyorum.
    Task<PagedResult<ContactMessage>> GetListAsync(ContactMessageListFilter filter, CancellationToken cancellationToken = default);

    // Burada reference number çakışmasını public değer üretiminde denetliyorum.
    Task<bool> ReferenceNumberExistsAsync(string referenceNumber, CancellationToken cancellationToken = default);

    // Burada süresi dolmuş idempotency kayıtlarını sınırlı batch ile siliyorum.
    Task<int> DeleteExpiredIdempotencyAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default);

    // Burada retention cutoff değerini geçen contact kayıtlarını bounded batch içinde anonimleştirmeye hazırlıyorum.
    Task<int> PrepareExpiredForAnonymizationAsync(DateTime cutoffUtc, DateTime utcNow, int batchSize, CancellationToken cancellationToken = default);
}

public sealed class ContactPrivacyOptions
{
    public const string SectionName = "ContactPrivacy";
    public string NoticeVersion { get; init; } = string.Empty;
    public DateTimeOffset NoticePublishedAtUtc { get; init; }
    public int? RetentionDays { get; init; }
    public int CleanupBatchSize { get; init; } = 100;
}

public sealed class ContactEmailOptions
{
    public string ContactInboxAddress { get; init; } = string.Empty;
    public string AdminContactMessageBaseUrl { get; init; } = string.Empty;
}

public sealed record ContactReceivedEmailPayload(
    string InboxAddress,
    string ReferenceNumber,
    string Name,
    string Email,
    string? Phone,
    ContactMessageSubject Subject,
    string? ProvidedOrderNumber,
    string Message,
    string? AdminDetailUrl);

public sealed record ContactReplyEmailPayload(
    string RecipientEmail,
    string RecipientName,
    string ReferenceNumber,
    string Body);

public interface IContactEmailPayloadReader
{
    // Burada worker'ın alınan iletişim mesajı için güvenilir payload'ı kaynaktan okumasını tanımlıyorum.
    Task<ContactReceivedEmailPayload?> GetReceivedAsync(Guid contactMessageId, CancellationToken cancellationToken = default);

    // Burada worker'ın müşteri yanıtı için güvenilir payload'ı kaynaktan okumasını tanımlıyorum.
    Task<ContactReplyEmailPayload?> GetReplyAsync(Guid replyId, CancellationToken cancellationToken = default);
}
