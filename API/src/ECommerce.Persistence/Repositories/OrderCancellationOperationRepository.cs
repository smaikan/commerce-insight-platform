using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class OrderCancellationOperationRepository : IOrderCancellationOperationRepository
{
    private readonly AppDbContext _context;

    // Burada cancellation saga kayıtlarını aynı scoped DbContext üzerinden yönetecek repository'yi hazırlıyorum.
    public OrderCancellationOperationRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada operasyon aggregate'ını app-generated kimlikleriyle açıkça Added durumda izliyorum.
    public async Task AddAsync(OrderCancellationOperation operation, CancellationToken cancellationToken = default)
    {
        await _context.OrderCancellationOperations.AddAsync(operation, cancellationToken);
    }

    // Burada siparişin son iptal operasyonunu refund item audit grafiğiyle getiriyorum.
    public Task<OrderCancellationOperation?> GetByOrderIdAsync(
        Guid orderId,
        bool forUpdate,
        CancellationToken cancellationToken = default)
    {
        return CreateGraphQuery(forUpdate)
            .Where(operation => operation.OrderId == orderId)
            .OrderByDescending(operation => operation.CreatedAt)
            .ThenByDescending(operation => operation.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Burada operasyonu kimliğiyle bütün item-level refund kayıtları dahil getiriyorum.
    public Task<OrderCancellationOperation?> GetByIdAsync(
        Guid operationId,
        bool forUpdate,
        CancellationToken cancellationToken = default)
    {
        return CreateGraphQuery(forUpdate)
            .FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken);
    }

    // Burada açık cancellation operasyonlarını lease zamanına göre bounded worker batch'ine seçiyorum.
    public async Task<IReadOnlyList<Guid>> GetDueIdsAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        return await _context.OrderCancellationOperations
            .AsNoTracking()
            .Where(operation =>
                (operation.Status == OrderCancellationOperationStatus.Requested ||
                 operation.Status == OrderCancellationOperationStatus.ReconciliationPending ||
                 operation.Status == OrderCancellationOperationStatus.Processing ||
                 operation.Status == OrderCancellationOperationStatus.ManualReview &&
                 operation.ErrorCode == OrderCancellationOperation.ProviderResponseMismatchErrorCode &&
                 operation.AttemptCount < OrderCancellationOperation.MaximumProviderVerificationAttempts) &&
                (!operation.NextAttemptAt.HasValue || operation.NextAttemptAt.Value <= utcNow))
            .OrderBy(operation => operation.NextAttemptAt)
            .ThenBy(operation => operation.CreatedAt)
            .ThenBy(operation => operation.Id)
            .Select(operation => operation.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    // Burada sorgunun update tracking seçimini tek yerde uygulayıp item ve payment snapshot'larını yüklüyorum.
    private IQueryable<OrderCancellationOperation> CreateGraphQuery(bool forUpdate)
    {
        var query = _context.OrderCancellationOperations
            .Include(operation => operation.Items)
            .ThenInclude(item => item.PaymentItemTransaction)
            .Include(operation => operation.Payment)
            .ThenInclude(payment => payment.ItemTransactions);
        return forUpdate ? query : query.AsNoTracking();
    }
}
