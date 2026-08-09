using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard.Dtos;
using MediatR;

namespace ECommerce.Application.Dashboard.Queries.GetDashboardProductAnalytics;

public sealed class GetDashboardProductAnalyticsQueryHandler
    : IRequestHandler<GetDashboardProductAnalyticsQuery, DashboardProductAnalyticsDto>
{
    private readonly IProductAnalyticsReader _reader;

    // Burada dashboard ürün analizini kalıcılık ayrıntısından bağımsız okuyucuyla hazırlıyorum.
    public GetDashboardProductAnalyticsQueryHandler(IProductAnalyticsReader reader)
    {
        _reader = reader;
    }

    // Burada seçili tarih aralığının toplu ürün analizini döndürüyorum.
    public Task<DashboardProductAnalyticsDto> Handle(
        GetDashboardProductAnalyticsQuery request,
        CancellationToken cancellationToken) =>
        _reader.GetDashboardProductAnalyticsAsync(request.From, request.To, cancellationToken);
}
