using ECommerce.API.Security;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.GuestSessions.Commands.ClaimGuestSession;
using ECommerce.Application.GuestSessions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.GuestSession;

[ApiController]
[Authorize]
[EnableRateLimiting("cart")]
[Route("api/guest-session")]
public sealed class GuestSessionController : ControllerBase
{
    private readonly ISender _sender;
    private readonly GuestSessionCookieManager _guestSessionCookies;

    // Burada ortak guest session claim isteğini Application katmanına iletecek bağımlılıkları hazırlıyorum.
    public GuestSessionController(
        ISender sender,
        GuestSessionCookieManager guestSessionCookies)
    {
        _sender = sender;
        _guestSessionCookies = guestSessionCookies;
    }

    // Burada login sonrasında guest cart ve favorites verilerini tek atomik işlemle kullanıcıya claim ediyorum.
    [HttpPost("claim")]
    [ProducesResponseType(typeof(GuestSessionClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GuestSessionClaimDto>> Claim(
        CancellationToken cancellationToken)
    {
        var sessionId = _guestSessionCookies.GetExistingSessionId(Request)
            ?? throw new ApiContractException(
                StatusCodes.Status400BadRequest,
                "guest_session_required",
                "Guest session required",
                "Claim işlemi için geçerli guest session cookie gereklidir.");
        var result = await _sender.Send(
            new ClaimGuestSessionCommand(sessionId),
            cancellationToken);
        _guestSessionCookies.DeleteSessionCookie(Response);
        return Ok(result);
    }
}
