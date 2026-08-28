using ECommerce.Application.Dashboard.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IAdminDashboardReader
{
    // Burada dashboard genel metriklerini salt okunur olarak getiriyorum.
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    // Burada admin menüsündeki işlem bekleyen kayıt sayaçlarını getiriyorum.
    Task<AdminWorkQueueSummaryDto> GetWorkQueueSummaryAsync(CancellationToken cancellationToken = default);
}
