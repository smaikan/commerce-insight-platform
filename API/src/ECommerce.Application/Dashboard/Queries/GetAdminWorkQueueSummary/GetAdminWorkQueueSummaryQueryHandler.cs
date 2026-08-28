using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetAdminWorkQueueSummary;

// Burada admin iş kuyruğu sorgusunu salt okunur dashboard okuyucusuna yönlendiriyorum.
public sealed class GetAdminWorkQueueSummaryQueryHandler : IRequestHandler<GetAdminWorkQueueSummaryQuery, AdminWorkQueueSummaryDto>
{
    private readonly IAdminDashboardReader _dashboardReader;

    // Burada iş kuyruğu sayaçlarını sağlayan okuyucuyu hazırlıyorum.
    public GetAdminWorkQueueSummaryQueryHandler(IAdminDashboardReader dashboardReader)
    {
        _dashboardReader = dashboardReader;
    }

    // Burada güncel admin iş kuyruğu özetini getiriyorum.
    public Task<AdminWorkQueueSummaryDto> Handle(GetAdminWorkQueueSummaryQuery request, CancellationToken cancellationToken) =>
        _dashboardReader.GetWorkQueueSummaryAsync(cancellationToken);
}
