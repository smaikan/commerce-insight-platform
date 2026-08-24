using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderCancellation;

public sealed record GetOrderCancellationQuery(Guid OrderId) : IRequest<OrderCancellationOperationDto>;
