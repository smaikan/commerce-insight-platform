using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IOrderCancellationOperationRepository
{
    // Burada yeni kalıcı iptal niyetini ve item-level refund kayıtlarını Added olarak izlemeyi tanımlıyorum.
    Task AddAsync(OrderCancellationOperation operation, CancellationToken cancellationToken = default);

    // Burada siparişin en güncel iptal operasyonunu owner-scoped servislerin kullanımı için getiriyorum.
    Task<OrderCancellationOperation?> GetByOrderIdAsync(
        Guid orderId,
        bool forUpdate,
        CancellationToken cancellationToken = default);

    // Burada worker veya polling akışının tek operasyonu item'larıyla getirmesini tanımlıyorum.
    Task<OrderCancellationOperation?> GetByIdAsync(
        Guid operationId,
        bool forUpdate,
        CancellationToken cancellationToken = default);

    // Burada reconciliation zamanı gelen operasyon kimliklerini bounded ve kararlı sırayla okuyorum.
    Task<IReadOnlyList<Guid>> GetDueIdsAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default);
}
