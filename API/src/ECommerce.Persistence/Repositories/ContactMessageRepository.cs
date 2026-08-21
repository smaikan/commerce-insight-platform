using ECommerce.Application.Common.Models;
using ECommerce.Application.Contacts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ContactMessageRepository : IContactMessageRepository, IContactEmailPayloadReader
{
    private readonly AppDbContext _context;
    private readonly ContactEmailOptions _emailOptions;

    // Burada iletişim persistence ve worker payload reader bağımlılıklarını hazırlıyorum.
    public ContactMessageRepository(AppDbContext context, ContactEmailOptions emailOptions)
    {
        _context = context;
        _emailOptions = emailOptions;
    }

    // Burada yeni iletişim aggregate'ını EF takibine ekliyorum.
    public async Task AddAsync(ContactMessage message, CancellationToken cancellationToken = default) =>
        await _context.ContactMessages.AddAsync(message, cancellationToken);

    // Burada submission idempotency kaydını takip etmeden getiriyorum.
    public Task<ContactSubmissionIdempotency?> GetSubmissionIdempotencyAsync(string keyHash, CancellationToken cancellationToken = default) =>
        _context.ContactSubmissionIdempotencies.AsNoTracking().FirstOrDefaultAsync(record => record.KeyHash == keyHash, cancellationToken);

    // Burada submission idempotency kaydını transaction takibiyle getiriyorum.
    public Task<ContactSubmissionIdempotency?> GetSubmissionIdempotencyForUpdateAsync(string keyHash, CancellationToken cancellationToken = default) =>
        _context.ContactSubmissionIdempotencies.FirstOrDefaultAsync(record => record.KeyHash == keyHash, cancellationToken);

    // Burada yeni submission idempotency sonucunu EF takibine ekliyorum.
    public async Task AddSubmissionIdempotencyAsync(ContactSubmissionIdempotency record, CancellationToken cancellationToken = default) =>
        await _context.ContactSubmissionIdempotencies.AddAsync(record, cancellationToken);

    // Burada admin detay grafiğini takip etmeden yüklüyorum.
    public Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DetailQuery().AsNoTracking().FirstOrDefaultAsync(message => message.Id == id, cancellationToken);

    // Burada admin mutation aggregate'ını audit ve reply grafiğiyle takipli yüklüyorum.
    public Task<ContactMessage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        DetailQuery().FirstOrDefaultAsync(message => message.Id == id, cancellationToken);

    // Burada iletişim listesini mesaj body üzerinde arama yapmadan kararlı biçimde sayfalıyorum.
    public async Task<PagedResult<ContactMessage>> GetListAsync(ContactMessageListFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<ContactMessage> query = _context.ContactMessages.AsNoTracking();
        if (filter.Status.HasValue) query = query.Where(message => message.Status == filter.Status.Value);
        if (filter.Subject.HasValue) query = query.Where(message => message.Subject == filter.Subject.Value);
        if (filter.AssignedAdminUserId.HasValue) query = query.Where(message => message.AssignedAdminUserId == filter.AssignedAdminUserId.Value);
        if (filter.CreatedFromUtc.HasValue) query = query.Where(message => message.CreatedAt >= filter.CreatedFromUtc.Value);
        if (filter.CreatedToUtc.HasValue) query = query.Where(message => message.CreatedAt <= filter.CreatedToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(message =>
                message.ReferenceNumber.Contains(search) ||
                message.Name.Contains(search) ||
                message.Email.Contains(search) ||
                (message.ProvidedOrderNumber != null && message.ProvidedOrderNumber.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(message => message.CreatedAt).ThenByDescending(message => message.Id)
            .Skip(checked((filter.PageNumber - 1) * filter.PageSize)).Take(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<ContactMessage>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    // Burada public reference numarasının benzersizliğini hızlı indeks sorgusuyla denetliyorum.
    public Task<bool> ReferenceNumberExistsAsync(string referenceNumber, CancellationToken cancellationToken = default) =>
        _context.ContactMessages.AsNoTracking().AnyAsync(message => message.ReferenceNumber == referenceNumber, cancellationToken);

    // Burada süresi dolan idempotency kayıtlarını sınırlı batch kimlikleriyle temizliyorum.
    public async Task<int> DeleteExpiredIdempotencyAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default)
    {
        var ids = await _context.ContactSubmissionIdempotencies.AsNoTracking()
            .Where(record => record.ExpiresAt <= utcNow).OrderBy(record => record.ExpiresAt).ThenBy(record => record.Id)
            .Select(record => record.Id).Take(batchSize).ToListAsync(cancellationToken);
        return ids.Count == 0
            ? 0
            : await _context.ContactSubmissionIdempotencies.Where(record => ids.Contains(record.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    // Burada retention süresi dolan aggregate ve contact outbox PII alanlarını bounded, takipli batch içinde anonimleştiriyorum.
    public async Task<int> PrepareExpiredForAnonymizationAsync(
        DateTime cutoffUtc,
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var ids = await _context.ContactMessages.AsNoTracking()
            .Where(message => message.AnonymizedAt == null && message.CreatedAt <= cutoffUtc)
            .OrderBy(message => message.CreatedAt).ThenBy(message => message.Id)
            .Select(message => message.Id).Take(batchSize).ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return 0;
        }

        var messages = await DetailQuery().Where(message => ids.Contains(message.Id)).ToListAsync(cancellationToken);
        var outboxMessages = await _context.EmailOutbox
            .Where(message => message.ContactMessageId.HasValue && ids.Contains(message.ContactMessageId.Value) &&
                (message.Type == EmailOutboxMessageType.ContactMessageReceived || message.Type == EmailOutboxMessageType.ContactMessageReply))
            .ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.AnonymizeForRetention(utcNow);
        }

        foreach (var outboxMessage in outboxMessages)
        {
            outboxMessage.AnonymizeContactDataForRetention(utcNow);
        }

        return messages.Count;
    }

    // Burada contact-received e-postası için body'yi outbox'ta çoğaltmadan kaynak tablodan okuyorum.
    public async Task<ContactReceivedEmailPayload?> GetReceivedAsync(Guid contactMessageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.ContactMessages.AsNoTracking().FirstOrDefaultAsync(item => item.Id == contactMessageId, cancellationToken);
        if (message is null) return null;
        var detailUrl = string.IsNullOrWhiteSpace(_emailOptions.AdminContactMessageBaseUrl)
            ? null
            : $"{_emailOptions.AdminContactMessageBaseUrl.TrimEnd('/')}/{message.Id}";
        return new ContactReceivedEmailPayload(
            _emailOptions.ContactInboxAddress,
            message.ReferenceNumber,
            message.Name,
            message.Email,
            message.Phone,
            message.Subject,
            message.ProvidedOrderNumber,
            message.Message,
            detailUrl);
    }

    // Burada contact-reply e-postası için alıcı ve body'yi güvenilir kaynak tablolardan okuyorum.
    public async Task<ContactReplyEmailPayload?> GetReplyAsync(Guid replyId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactMessageReplies.AsNoTracking()
            .Where(reply => reply.Id == replyId)
            .Select(reply => new ContactReplyEmailPayload(
                reply.ContactMessage.Email,
                reply.ContactMessage.Name,
                reply.ContactMessage.ReferenceNumber,
                reply.Body))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Burada admin detay ve mutation işlemleri için gerekli tam grafiği tek sorgu tanımıyla hazırlıyorum.
    private IQueryable<ContactMessage> DetailQuery() =>
        _context.ContactMessages
            .Include(message => message.Activities)
            .Include(message => message.Replies).ThenInclude(reply => reply.OutboxMessage)
            .AsSplitQuery();
}
