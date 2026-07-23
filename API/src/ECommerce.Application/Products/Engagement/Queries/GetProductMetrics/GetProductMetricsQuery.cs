using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;

public sealed record GetProductMetricsQuery(long ProductId, DateOnly From, DateOnly To) : IRequest<IReadOnlyList<ProductMetricDto>>;
