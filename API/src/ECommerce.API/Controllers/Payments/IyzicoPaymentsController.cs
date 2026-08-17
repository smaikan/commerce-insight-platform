using ECommerce.Application.Common.Payments;
using ECommerce.Application.Payments;
using ECommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Controllers.Payments;

[ApiController]
[Route("api/payments/iyzico")]
public sealed class IyzicoPaymentsController : ControllerBase
{
    private readonly CheckoutFormPaymentService _payments;
    private readonly IyzicoOptions _options;

    // Burada iyzico callback/webhook uçlarını ortak sonuçlandırma servisine bağlıyorum.
    public IyzicoPaymentsController(
        CheckoutFormPaymentService payments,
        IOptions<IyzicoOptions> options)
    {
        _payments = payments;
        _options = options.Value;
    }

    // Burada iyzico'nun tarayıcı POST callback tokenını tekrar sorgulayıp güvenli frontend sonucuna yönlendiriyorum.
    [HttpPost("callback")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status303SeeOther)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Callback(
        [FromForm] IyzicoCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payments.CompleteByTokenAsync(request.Token, cancellationToken);
        var returnUrl = QueryHelpers.AddQueryString(
            _options.ReturnUrl,
            new Dictionary<string, string?>
            {
                ["paymentId"] = result.PaymentId.ToString("D"),
                ["orderId"] = result.OrderId.ToString("D"),
                ["status"] = result.Status.ToString()
            });
        Response.Headers.Location = returnUrl;
        return StatusCode(StatusCodes.Status303SeeOther);
    }

    // Burada V3 imzalı iyzico webhook bildirimini idempotent retrieve ile sonuçlandırıyorum.
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Webhook(
        IyzicoWebhookRequest request,
        [FromHeader(Name = "X-IYZ-SIGNATURE-V3"), System.ComponentModel.DataAnnotations.Required] string? signature,
        CancellationToken cancellationToken)
    {
        await _payments.CompleteWebhookAsync(
            new CheckoutFormWebhookNotification(
                request.IyziEventType,
                request.IyziPaymentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                request.Token,
                request.PaymentConversationId,
                request.Status),
            signature ?? string.Empty,
            cancellationToken);
        return NoContent();
    }
}

public sealed record IyzicoCallbackRequest(
    [property: System.ComponentModel.DataAnnotations.Required] string Token);

public sealed record IyzicoWebhookRequest(
    [property: System.ComponentModel.DataAnnotations.Required] string IyziEventType,
    [property: System.ComponentModel.DataAnnotations.Range(1, long.MaxValue)] long IyziPaymentId,
    [property: System.ComponentModel.DataAnnotations.Required] string Token,
    [property: System.ComponentModel.DataAnnotations.Required] string PaymentConversationId,
    [property: System.ComponentModel.DataAnnotations.Required] string Status);
