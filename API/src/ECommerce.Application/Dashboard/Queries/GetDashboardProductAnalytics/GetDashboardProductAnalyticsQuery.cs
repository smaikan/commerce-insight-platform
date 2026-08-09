using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetDashboardProductAnalytics;

public sealed record GetDashboardProductAnalyticsQuery(DateOnly From, DateOnly To)
    : IRequest<DashboardProductAnalyticsDto>;
