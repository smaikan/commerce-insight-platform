using ECommerce.API.Security;
using ECommerce.Application.Orders.Commands.ExpireStockReservations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Order;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[EnableRateLimiting("orders")]
[Route("api/orders/reservations")]
public sealed class StockReservationMaintenanceController : ControllerBase
{
    private readonly ISender _sender;

    // Burada yönetim kaynaklı rezervasyon bakım isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public StockReservationMaintenanceController(ISender sender)
    {
        _sender = sender;
    }

    // Burada yöneticinin hosted worker dışında da süre dolmuş rezervasyonları sınırlı partiyle temizlemesini sağlıyorum.
    [HttpPost("expire")]
    public async Task<ActionResult<StockReservationExpirationResult>> Expire(
        ExpireStockReservationsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ExpireStockReservationsCommand(request.BatchSize), cancellationToken));
}

// Burada manuel stok rezervasyonu bakım isteğinin sınırlı parti boyutunu tanımlıyorum.
public sealed record ExpireStockReservationsRequest(int BatchSize = 100);
