using ECommerce.Application.Dashboard.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductAnalyticsReader
{
    // Burada dashboard ürün analizini tarih aralığına göre topluca okuma sözleşmesini tanımlıyorum.
    Task<DashboardProductAnalyticsDto> GetDashboardProductAnalyticsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
