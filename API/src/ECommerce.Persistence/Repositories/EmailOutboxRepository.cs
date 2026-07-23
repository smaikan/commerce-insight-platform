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

    // Burada gönderim zamanı gelen ve süresi geçmemiş e-postaları işlenmek üzere getiriyorum.
    public async Task<IReadOnlyList<EmailOutboxMessage>> GetPendingForUpdateAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await _context.EmailOutbox
            .Where(message =>
                message.ProcessedAt == null &&
                message.NextAttemptAt <= utcNow &&
                (!message.ExpiresAt.HasValue || message.ExpiresAt > utcNow))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
}
