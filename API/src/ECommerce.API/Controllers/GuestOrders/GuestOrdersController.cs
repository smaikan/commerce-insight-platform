using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.GuestOrders;

[ApiController]
[Route("api/guest-orders")]
public sealed class GuestOrdersController : ControllerBase
{
    private const string SessionCookieName = "ecommerce_guest_orders";
    private const string CsrfCookieName = "ecommerce_guest_csrf";
    private readonly GuestOrderAccessService _access;
    private readonly GuestOrderOperationsService _operations;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    // Burada guest sipariş erişim ve self-service HTTP uçlarının bağımlılıklarını hazırlıyorum.
    public GuestOrdersController(
        GuestOrderAccessService access,
        GuestOrderOperationsService operations,
        ICurrentUserService currentUser,
        IConfiguration configuration)
    {
        _access = access;
        _operations = operations;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    // Burada sipariş numarası ve e-postayı yetkiye çevirmeden her zaman aynı 202 cevabıyla magic-link kuyruğa alıyorum.
    [HttpPost("access-links")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestAccessLink(
        GuestAccessLinkRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        await _access.RequestAccessLinkAsync(
            request.OrderNumber,
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            cancellationToken);
        return Accepted(new { message = "Sipariş eşleşirse erişim bağlantısı e-posta kuyruğuna alındı." });
    }

    // Burada URL fragment'ından body ile gelen tek kullanımlık tokenı guest session cookie'lerine çeviriyorum.
    [HttpPost("access/exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<GuestAccessExchangeResponse>> Exchange(
        GuestAccessExchangeRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        var result = await _access.ExchangeAsync(
            request.Token,
            GetCookie(SessionCookieName),
            cancellationToken);
        if (result.NewSessionToken is not null && result.NewCsrfToken is not null)
        {
            WriteSessionCookies(result.NewSessionToken, result.NewCsrfToken, result.SessionExpiresAt);
        }

        return Ok(new GuestAccessExchangeResponse(result.OrderId, result.SessionExpiresAt));
    }

    // Burada guest session'ın erişebildiği siparişleri sayfalı ve no-store cevabıyla getiriyorum.
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePage(pageNumber, pageSize);
        return Ok(await _access.GetOrdersAsync(RequireSession(), pageNumber, pageSize, cancellationToken));
    }

    // Burada session grant'i olmayan siparişlerde 404 vererek guest sipariş detayını getiriyorum.
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id, CancellationToken cancellationToken) =>
        Ok(await _access.GetOrderAsync(RequireSession(), id, cancellationToken));

    // Burada guest sipariş için CSRF ve origin korumalı idempotent ödeme denemesi başlatıyorum.
    [HttpPost("{id:guid}/payments")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        Guid id,
        GuestPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        var csrf = RequireCsrf();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new BadHttpRequestException("Idempotency-Key header is required.");
        }

        return Ok(await _operations.CreatePaymentAsync(
            RequireSession(), csrf, id, request.Provider, idempotencyKey, cancellationToken));
    }

    // Burada guest müşterinin ödeme öncesi siparişini CSRF ve origin korumasıyla iptal ediyorum.
    [HttpPost("{id:guid}/cancel")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        return Ok(await _operations.CancelAsync(
            RequireSession(), RequireCsrf(), id, cancellationToken));
    }

    // Burada guest siparişin iade taleplerini session grant'i altında sayfalıyorum.
    [HttpGet("{id:guid}/returns")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<PagedResult<ReturnRequestSummaryDto>>> GetReturns(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePage(pageNumber, pageSize);
        return Ok(await _operations.GetReturnsAsync(
            RequireSession(), id, pageNumber, pageSize, cancellationToken));
    }

    // Burada guest müşterinin teslim edilmiş siparişi için CSRF korumalı iade veya değişim talebi oluşturuyorum.
    [HttpPost("{id:guid}/returns")]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnRequestDto>> CreateReturn(
        Guid id,
        GuestReturnRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        var result = await _operations.CreateReturnAsync(
            RequireSession(),
            RequireCsrf(),
            id,
            request.Type,
            (request.Items ?? []).Select(item => new CreateReturnItemCommand(
                item.OrderItemId, item.Quantity, item.ReplacementProductVariantId)).ToList(),
            request.CustomerNote,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // Burada guest session'ın yalnız kendi siparişindeki tek iade talebi detayını getiriyorum.
    [HttpGet("{id:guid}/returns/{returnId:guid}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ReturnRequestDto>> GetReturn(
        Guid id,
        Guid returnId,
        CancellationToken cancellationToken) =>
        Ok(await _operations.GetReturnAsync(RequireSession(), id, returnId, cancellationToken));

    // Burada JWT hesabı ve doğrulanmış aynı e-posta şartıyla bütün guest siparişleri hesaba bağlıyorum.
    [Authorize]
    [HttpPost("claim")]
    public async Task<ActionResult<GuestClaimResponse>> Claim(CancellationToken cancellationToken)
    {
        EnsureTrustedOrigin();
        var claimedCount = await _access.ClaimAsync(
            RequireSession(), RequireCsrf(), _currentUser.GetRequiredUserId(), cancellationToken);
        DeleteSessionCookies();
        return Ok(new GuestClaimResponse(claimedCount));
    }

    // Burada eksik guest session cookie'sini güvenli 401 sözleşmesine dönüştürüyorum.
    private string RequireSession() => GetCookie(SessionCookieName)
        ?? throw new ApiContractException(401, "invalid_guest_access", "Guest access required", "Geçerli guest sipariş oturumu gereklidir.");

    // Burada double-submit CSRF cookie ve header değerlerinin birebir eşleşmesini zorunlu tutuyorum.
    private string RequireCsrf()
    {
        var cookie = GetCookie(CsrfCookieName);
        var header = Request.Headers["X-Guest-CSRF"].ToString();
        if (cookie is null || string.IsNullOrWhiteSpace(header) || !string.Equals(cookie, header, StringComparison.Ordinal))
        {
            throw new ApiContractException(403, "invalid_guest_access", "CSRF validation failed", "Guest mutasyonu için CSRF doğrulaması başarısız oldu.");
        }

        return header;
    }

    // Burada yalnız kanonik 256 bit guest cookie değerini kabul ediyorum.
    private string? GetCookie(string name)
    {
        return Request.Cookies.TryGetValue(name, out var value) &&
               value.Length == 64 &&
               value.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F')
            ? value
            : null;
    }

    // Burada cookie tabanlı mutasyonlarda Origin'i yapılandırılmış storefront allowlist'iyle doğruluyorum.
    private void EnsureTrustedOrigin()
    {
        var origin = Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new ApiContractException(403, "invalid_guest_access", "Origin validation failed", "Guest mutasyonu için Origin header zorunludur.");
        }

        var trustedOrigins = (_configuration["GuestProtection:TrustedOrigins"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!trustedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ApiContractException(403, "invalid_guest_access", "Origin validation failed", "İstek origin'i güvenilir değil.");
        }
    }

    // Burada guest listelemelerinde geçersiz veya aşırı sayfa değerlerini reddediyorum.
    private static void EnsurePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
        {
            throw new BadHttpRequestException("Page number and page size are invalid.");
        }
    }

    // Burada guest session ve CSRF cookie'lerini yedi günlük güvenli seçeneklerle yazıyorum.
    private void WriteSessionCookies(string sessionToken, string csrfToken, DateTime expiresAt)
    {
        var options = CreateCookieOptions(expiresAt);
        Response.Cookies.Append(SessionCookieName, sessionToken, options);
        Response.Cookies.Append(CsrfCookieName, csrfToken, options);
    }

    // Burada claim sonrası artık geçersiz guest cookie'lerini browser'dan siliyorum.
    private void DeleteSessionCookies()
    {
        var options = CreateCookieOptions(DateTime.UtcNow.AddDays(-1));
        Response.Cookies.Delete(SessionCookieName, options);
        Response.Cookies.Delete(CsrfCookieName, options);
    }

    // Burada guest cookie'leri için Secure, HttpOnly, SameSite=Lax ve API path seçeneklerini oluşturuyorum.
    private static CookieOptions CreateCookieOptions(DateTime expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/api",
        Expires = new DateTimeOffset(expiresAt),
        MaxAge = expiresAt > DateTime.UtcNow ? expiresAt - DateTime.UtcNow : TimeSpan.Zero
    };
}

// Burada magic-link istemek için kullanılan ve yetki sağlamayan sipariş/e-posta girdisini tanımlıyorum.
public sealed record GuestAccessLinkRequest(string OrderNumber, string Email);

// Burada URL fragment'ından BFF body alanına taşınan tek kullanımlık tokenı tanımlıyorum.
public sealed record GuestAccessExchangeRequest(string Token);

// Burada exchange sonrasında açılan sipariş ve session son kullanma zamanını döndürüyorum.
public sealed record GuestAccessExchangeResponse(Guid OrderId, DateTime SessionExpiresAt);

// Burada guest ödeme isteğinin frontend tarafından seçilebilen sağlayıcı alanını tanımlıyorum.
public sealed record GuestPaymentRequest(PaymentProvider Provider);

// Burada guest iade veya değişim isteğinin güvenilir sipariş kalemi alanlarını tanımlıyorum.
public sealed record GuestReturnRequest(
    ReturnType Type,
    IReadOnlyList<GuestReturnItemRequest> Items,
    string? CustomerNote = null);

// Burada guest iade kaleminin adet ve opsiyonel değişim varyantını tanımlıyorum.
public sealed record GuestReturnItemRequest(Guid OrderItemId, int Quantity, Guid? ReplacementProductVariantId = null);

// Burada claim işleminde hesaba bağlanan sipariş sayısını döndürüyorum.
public sealed record GuestClaimResponse(int ClaimedOrderCount);
