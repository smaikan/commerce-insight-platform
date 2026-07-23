using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IEmailOutboxRepository
{
    // Burada e-posta mesajının kalıcı kuyruğa eklenme sözleşmesini tanımlıyorum.
    Task AddAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default);

    // Burada gönderilmeye hazır e-posta batch'inin alınma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<EmailOutboxMessage>> GetPendingForUpdateAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);
}
