using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetDashboardOverview;

public sealed class GetDashboardOverviewQueryHandler : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IAdminDashboardReader _dashboardReader;

    // Burada dashboard verisini kalıcılık ayrıntısından bağımsız okuyucuyla hazırlıyorum.
    public GetDashboardOverviewQueryHandler(IAdminDashboardReader dashboardReader)
    {
        _dashboardReader = dashboardReader;
    }

    // Burada yönetim panelinin gerçek aggregate metriklerini döndürüyorum.
    public Task<DashboardOverviewDto> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken) =>
        _dashboardReader.GetOverviewAsync(cancellationToken);
}
