using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IEmailOutboxRepository
{
    // Burada e-posta mesajının kalıcı kuyruğa eklenme sözleşmesini tanımlıyorum.
    Task AddAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default);

    // Burada gönderilmeye hazır e-posta batch'inin alınma sözleşmesini tanımlıyorum.
    // Burada uygun mesajları belirli worker için atomik olarak lease'leme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<EmailOutboxMessage>> ClaimPendingAsync(
        string workerId,
        DateTime utcNow,
        DateTime leaseExpiresAt,
        int batchSize,
        CancellationToken cancellationToken = default);

    // Burada aktif lease'i olmayan süresi geçmiş mesajları sınırlı partiler halinde terminal dead-letter durumuna alma sözleşmesini tanımlıyorum.
    Task<int> ExpirePendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    // Burada yalnızca geçerli lease sahibi worker'ın teslimi tamamlayabilmesini tanımlıyorum.
    Task<bool> CompleteClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    // Burada SMTP öncesi claim lease'ini uzatıp sırada bekleyen mesajın başka instance'a düşmesini engelleme sözleşmesini tanımlıyorum.
    Task<bool> RenewClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default);

    // Burada aktif lease sahibi worker'ın SMTP öncesi veya sonrası süresi geçen mesajı terminal duruma alabilmesini tanımlıyorum.
    Task<bool> ExpireClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    // Burada yalnızca geçerli lease sahibi worker'ın hatayı kaydedip yeniden deneme planlayabilmesini tanımlıyorum.
    Task<bool> FailClaimAsync(
        Guid messageId,
        Guid claimToken,
        string workerId,
        DateTime utcNow,
        string error,
        CancellationToken cancellationToken = default);
}
