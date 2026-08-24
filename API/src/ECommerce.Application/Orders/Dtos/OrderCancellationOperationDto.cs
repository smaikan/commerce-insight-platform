using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Dtos;

public sealed record OrderCancellationOperationDto(
    Guid OperationId,
    Guid OrderId,
    OrderCancellationOperationStatus Status,
    PaymentReversalType ReversalType,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? NextAttemptAt,
    string PollingUrl);

public sealed record OrderCancellationResult(
    OrderDto? Order,
    OrderCancellationOperationDto? Operation)
{
    public bool IsCompleted => Order is not null;
    public OrderStatus? Status => Order?.Status;
}
