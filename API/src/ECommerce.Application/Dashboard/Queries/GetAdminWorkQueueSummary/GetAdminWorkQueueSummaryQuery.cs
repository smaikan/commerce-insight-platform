using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetAdminWorkQueueSummary;

// Burada admin iş kuyruğu sayaçlarını okumak için sorgu sözleşmesini tanımlıyorum.
public sealed record GetAdminWorkQueueSummaryQuery : IRequest<AdminWorkQueueSummaryDto>;
