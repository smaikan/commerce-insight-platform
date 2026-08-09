using ECommerce.Application.Dashboard.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IAdminDashboardReader
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
