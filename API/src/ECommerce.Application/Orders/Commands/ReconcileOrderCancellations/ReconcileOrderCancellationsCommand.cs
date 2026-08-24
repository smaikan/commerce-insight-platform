using MediatR;

namespace ECommerce.Application.Orders.Commands.ReconcileOrderCancellations;

public sealed record ReconcileOrderCancellationsCommand(int BatchSize)
    : IRequest<OrderCancellationReconciliationResult>;

public sealed record OrderCancellationReconciliationResult(int ClaimedCount, int CompletedCount);
