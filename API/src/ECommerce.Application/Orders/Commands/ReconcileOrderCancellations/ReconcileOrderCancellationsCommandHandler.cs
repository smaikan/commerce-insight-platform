using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ReconcileOrderCancellations;

public sealed class ReconcileOrderCancellationsCommandHandler
    : IRequestHandler<ReconcileOrderCancellationsCommand, OrderCancellationReconciliationResult>
{
    private readonly IOrderCancellationOperationRepository _operations;
    private readonly OrderCancellationService _cancellations;
    private readonly IDateTimeProvider _clock;

    // Burada bounded cancellation reconciliation batch'inin repository, servis ve saat bağımlılıklarını hazırlıyorum.
    public ReconcileOrderCancellationsCommandHandler(
        IOrderCancellationOperationRepository operations,
        OrderCancellationService cancellations,
        IDateTimeProvider clock)
    {
        _operations = operations;
        _cancellations = cancellations;
        _clock = clock;
    }

    // Burada zamanı gelen operasyonları kararlı bounded sırayla işleyip tek başarısızlığın batch'i durdurmasını engelliyorum.
    public async Task<OrderCancellationReconciliationResult> Handle(
        ReconcileOrderCancellationsCommand request,
        CancellationToken cancellationToken)
    {
        var batchSize = Math.Clamp(request.BatchSize, 1, 100);
        var ids = await _operations.GetDueIdsAsync(_clock.UtcNow, batchSize, cancellationToken);
        var completed = 0;
        foreach (var id in ids)
        {
            if (await _cancellations.ProcessAsync(id, cancellationToken))
            {
                completed++;
            }
        }

        return new OrderCancellationReconciliationResult(ids.Count, completed);
    }
}
