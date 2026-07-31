using System.Data;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly AppDbContext _context;

    // Burada e-posta kuyruğu repository'sini aynı istek kapsamındaki DbContext ile hazırlıyorum.
    public EmailOutboxRepository(AppDbContext context) => _context = context;

    // Burada e-posta mesajını SMTP çağrısı yapmadan kalıcı kuyruğa ekliyorum.
    public async Task AddAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default) =>
        await _context.EmailOutbox.AddAsync(message, cancellationToken);

    // Burada uygun mesajları serializable işlemde worker'a atomik olarak claim edip SMTP'den önce kalıcılaştırıyorum.
    public async Task<IReadOnlyList<EmailOutboxMessage>> ClaimPendingAsync(
        string workerId,
        DateTime utcNow,
        DateTime leaseExpiresAt,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerId))
        {
            throw new ArgumentException("Outbox worker id is required.", nameof(workerId));
        }

        if (leaseExpiresAt <= utcNow)
        {
            throw new ArgumentException("Outbox lease expiry must be in the future.", nameof(leaseExpiresAt));
        }

        if (batchSize <= 0)
        {
            return Array.Empty<EmailOutboxMessage>();
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var messages = await _context.EmailOutbox
                .Where(message =>
                    message.ProcessedAt == null &&
                    message.DeadLetteredAt == null &&
                    message.NextAttemptAt <= utcNow &&
                    (!message.ExpiresAt.HasValue || message.ExpiresAt > utcNow) &&
                    (!message.LeaseExpiresAt.HasValue || message.LeaseExpiresAt <= utcNow))
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                message.ClaimForProcessing(workerId, Guid.NewGuid(), leaseExpiresAt, utcNow);
            }

            if (messages.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (IReadOnlyList<EmailOutboxMessage>)messages;
        });
    }

    // Burada aktif lease'i bulunmayan süresi geçmiş mesajları serializable transaction içinde sınırlı olarak terminal dead-letter durumuna alıyorum.
    public async Task<int> ExpirePendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            return 0;
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var expiredMessages = await _context.EmailOutbox
                .Where(message =>
                    message.ProcessedAt == null &&
                    message.DeadLetteredAt == null &&
                    message.ExpiresAt.HasValue &&
                    message.ExpiresAt.Value <= utcNow &&
                    (!message.LeaseExpiresAt.HasValue || message.LeaseExpiresAt.Value <= utcNow))
                .OrderBy(message => message.ExpiresAt)
                .ThenBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in expiredMessages)
            {
                message.MarkExpired(utcNow);
            }

            if (expiredMessages.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return expiredMessages.Count;
        });
    }

    // Burada yalnızca geçerli claim sahibinin mesajı tamamlamasına izin veriyorum.
    public async Task<bool> CompleteClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var message = await GetActiveClaimForUpdateAsync(
            messageId,
            claimToken,
            workerId,
            utcNow,
            cancellationToken);

        if (message is null)
        {
            return false;
        }

        message.MarkProcessed(utcNow);
        return await TrySaveClaimChangeAsync(message, cancellationToken);
    }

    // Burada yalnız aktif claim sahibinin SMTP çağrısından önce lease süresini atomik olarak yenilemesine izin veriyorum.
    public async Task<bool> RenewClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var message = await GetActiveClaimForUpdateAsync(
            messageId,
            claimToken,
            workerId,
            utcNow,
            cancellationToken);

        if (message is null || !message.RenewClaim(workerId, claimToken, leaseExpiresAt, utcNow))
        {
            return false;
        }

        return await TrySaveClaimChangeAsync(message, cancellationToken);
    }

    // Burada yalnızca aktif claim sahibi worker'ın artık süresi geçmiş mesajı SMTP göndermeden terminal duruma almasına izin veriyorum.
    public async Task<bool> ExpireClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var message = await GetOwnedExpiredClaimForUpdateAsync(
            messageId,
            claimToken,
            workerId,
            utcNow,
            cancellationToken);

        if (message is null || !message.IsExpired(utcNow))
        {
            return false;
        }

        message.MarkExpired(utcNow);
        return await TrySaveClaimChangeAsync(message, cancellationToken);
    }

    // Burada yalnızca geçerli claim sahibinin hatayı kaydedip mesajı yeniden denemeye bırakmasına izin veriyorum.
    public async Task<bool> FailClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        string error,
        CancellationToken cancellationToken = default)
    {
        var message = await GetActiveClaimForUpdateAsync(
            messageId,
            claimToken,
            workerId,
            utcNow,
            cancellationToken);

        if (message is null)
        {
            return false;
        }

        message.RecordFailure(utcNow, error);
        return await TrySaveClaimChangeAsync(message, cancellationToken);
    }

    // Burada mesajın worker, token ve aktif lease bilgisiyle gerçekten sahiplenildiğini takipli olarak doğruluyorum.
    private Task<EmailOutboxMessage?> GetActiveClaimForUpdateAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return _context.EmailOutbox.FirstOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.ProcessedAt == null &&
                message.DeadLetteredAt == null &&
                message.ClaimToken == claimToken &&
                message.ProcessingWorker == workerId.Trim() &&
                message.LeaseExpiresAt.HasValue &&
                message.LeaseExpiresAt > utcNow,
            cancellationToken);
    }

    // Burada eşzamanlı olarak kaybedilen claim'i başarısızlık yerine sessizce sahiplik kaybı olarak ele alıyorum.
    // Burada lease tam o anda bitmiş olsa bile aynı worker'ın sahip olduğu süresi geçmiş mesajı güvenle terminalleştirmek için çözümlüyorum.
    private Task<EmailOutboxMessage?> GetOwnedExpiredClaimForUpdateAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return _context.EmailOutbox.FirstOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.ProcessedAt == null &&
                message.DeadLetteredAt == null &&
                message.ClaimToken == claimToken &&
                message.ProcessingWorker == workerId.Trim() &&
                message.ExpiresAt.HasValue &&
                message.ExpiresAt.Value <= utcNow,
            cancellationToken);
    }

    private async Task<bool> TrySaveClaimChangeAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.Entry(message).State = EntityState.Detached;
            return false;
        }
    }
}
