using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetDashboardOverview;

// Burada yönetim dashboard özetini istemek için parametresiz sorguyu tanımlıyorum.
public sealed record GetDashboardOverviewQuery : IRequest<DashboardOverviewDto>;
