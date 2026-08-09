using ECommerce.API.Security;
using ECommerce.Application.Dashboard.Dtos;
using ECommerce.Application.Dashboard.Queries.GetDashboardProductAnalytics;
using ECommerce.Application.Dashboard.Queries.GetDashboardOverview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Dashboard;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    // Burada dashboard HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    // Burada yönetici için gerçek aggregate operasyon metriklerini getiriyorum.
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetDashboardOverviewQuery(), cancellationToken));

    // Burada yöneticinin seçtiği UTC gün aralığı için tüm ürünlerin toplu analizini getiriyorum.
    [HttpGet("product-analytics")]
    public async Task<ActionResult<DashboardProductAnalyticsDto>> GetProductAnalytics(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetDashboardProductAnalyticsQuery(from, to), cancellationToken));
}
