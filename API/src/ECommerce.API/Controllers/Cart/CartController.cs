using ECommerce.Application.Carts.Commands.AddCartItem;
using ECommerce.API.Security;
using ECommerce.Application.Carts.Commands.ClearCart;
using ECommerce.Application.Carts.Commands.MergeGuestCart;
using ECommerce.Application.Carts.Commands.RemoveCartItem;
using ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Queries.GetCart;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders.Checkout;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Cart;

[ApiController]
[EnableRateLimiting("cart")]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private const string GuestOrderSessionCookieName = "ecommerce_guest_orders";
    private const string GuestOrderCsrfCookieName = "ecommerce_guest_csrf";
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;
    private readonly GuestSessionCookieManager _guestSessionCookies;

    // Burada Cart HTTP isteklerini Application akışlarına yönlendirecek sender'ı hazırlıyorum.
    public CartController(
        ISender sender,
        IConfiguration configuration,
        GuestSessionCookieManager guestSessionCookies)
    {
        _sender = sender;
        _configuration = configuration;
        _guestSessionCookies = guestSessionCookies;
    }

    // Burada giriş yapan kullanıcıya veya güvenli misafir cookie'sine ait güncel sepeti getiriyorum.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetCartQuery(GetSessionIdForCartAccess()),
            cancellationToken));

    // Burada istemcinin yalnız varyant ve adet göndermesine izin vererek ürünü sepete ekliyorum.
    [AllowAnonymous]
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        AddCartItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new AddCartItemCommand(
                request.ProductVariantId,
                request.Quantity,
                GetSessionIdForCartAccess(),
                request.ExpectedConcurrencyToken),
            cancellationToken));

    // Burada owner'a ait satırın adedini istemcinin concurrency tokenıyla güncelliyorum.
    [AllowAnonymous]
    [HttpPut("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItemQuantity(
        Guid cartItemId,
        UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new UpdateCartItemQuantityCommand(
                cartItemId,
                request.Quantity,
                request.ExpectedConcurrencyToken,
                GetSessionIdForCartAccess()),
            cancellationToken));

    // Burada owner'a ait satırı query'den gelen concurrency tokenıyla sepetten kaldırıyorum.
    [AllowAnonymous]
    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(
        Guid cartItemId,
        [FromQuery] Guid expectedConcurrencyToken,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new RemoveCartItemCommand(
                cartItemId,
                expectedConcurrencyToken,
                GetSessionIdForCartAccess()),
            cancellationToken));

    // Burada owner'a ait tüm sepet satırlarını query'den gelen concurrency tokenıyla temizliyorum.
    [AllowAnonymous]
    [HttpDelete]
    public async Task<ActionResult<CartDto>> ClearCart(
        [FromQuery] Guid expectedConcurrencyToken,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new ClearCartCommand(
                expectedConcurrencyToken,
                GetSessionIdForCartAccess()),
            cancellationToken));

    // Burada giriş yapan kullanıcının güvenli misafir cookie'sindeki sepeti kendi sepetiyle birleştiriyorum.
    [Authorize]
    [HttpPost("merge-guest")]
    public async Task<ActionResult<CartDto>> MergeGuestCart(CancellationToken cancellationToken)
    {
        var cart = await _sender.Send(
            new MergeGuestCartCommand(GetExistingGuestCartSessionId() ?? string.Empty),
            cancellationToken);
        DeleteGuestCartCookie();
        return Ok(cart);
    }

    // Burada anonim sepeti zorunlu müşteri, adres, aktif kargo ve idempotency verileriyle siparişe dönüştürüyorum.
    [AllowAnonymous]
    [HttpPost("checkout/guest")]
    public async Task<ActionResult<OrderDto>> GuestCheckout(
        GuestCheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Turnstile-Token")] string? turnstileToken,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            throw new ConflictException("Authenticated customers must use the member checkout endpoint.");
        }

        EnsureGuestCheckoutOrigin();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new BadHttpRequestException("Idempotency-Key header is required.");
        }

        var cartSessionId = _guestSessionCookies.GetSessionIdForAccess(HttpContext)!;
        var result = await _sender.Send(
            new CreateGuestOrderCommand(
                cartSessionId,
                GetCanonicalCookie(GuestOrderSessionCookieName),
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                turnstileToken,
                idempotencyKey,
                request.ExpectedCartConcurrencyToken,
                new CheckoutCustomerInput(
                    request.Customer.FirstName,
                    request.Customer.LastName,
                    request.Customer.Email,
                    request.Customer.PhoneNumber),
                request.ShippingAddress.ToInput(AddressType.Shipping),
                request.BillingAddress?.ToInput(AddressType.Billing),
                request.ShippingMethodId,
                request.CouponCode),
            cancellationToken);
        if (result.NewSessionToken is not null && result.NewCsrfToken is not null && result.SessionExpiresAt.HasValue)
        {
            WriteGuestOrderCookies(result.NewSessionToken, result.NewCsrfToken, result.SessionExpiresAt.Value);
        }

        return result.WasReplay
            ? Ok(result.Order)
            : StatusCode(StatusCodes.Status201Created, result.Order);
    }

    // Burada giriş yapmış kullanıcıda cookie'yi yok sayıp anonim istekte güvenli guest session üretiyorum veya okuyorum.
    private string? GetSessionIdForCartAccess()
    {
        return _guestSessionCookies.GetSessionIdForAccess(HttpContext);
    }

    // Burada yalnız geçerli biçimdeki daha önce verilmiş misafir sepet oturumunu okuyorum.
    private string? GetExistingGuestCartSessionId()
    {
        return _guestSessionCookies.GetExistingSessionId(Request);
    }

    // Burada yalnız sunucunun ürettiği 256 bitlik guest güvenlik cookie değerini kabul ediyorum.
    private string? GetCanonicalCookie(string name)
    {
        return Request.Cookies.TryGetValue(name, out var value) && GuestSessionCookieManager.IsCanonicalToken(value)
            ? value
            : null;
    }

    // Burada misafir sepeti kullanıcıya devredildiğinde artık geçersiz olan browser cookie'sini siliyorum.
    private void DeleteGuestCartCookie()
    {
        _guestSessionCookies.DeleteSessionCookie(Response);
    }

    // Burada guest sipariş session ve CSRF cookie'lerini yedi günlük güvenli seçeneklerle yazıyorum.
    private void WriteGuestOrderCookies(string sessionToken, string csrfToken, DateTime expiresAt)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/api",
            Expires = new DateTimeOffset(expiresAt),
            MaxAge = expiresAt - DateTime.UtcNow
        };
        Response.Cookies.Append(GuestOrderSessionCookieName, sessionToken, options);
        Response.Cookies.Append(GuestOrderCsrfCookieName, csrfToken, options);
    }

    // Burada guest checkout cookie mutasyonunun yalnız yapılandırılmış storefront origin'inden gelmesini sağlıyorum.
    private void EnsureGuestCheckoutOrigin()
    {
        var origin = Request.Headers.Origin.ToString();
        var trustedOrigins = (_configuration["GuestProtection:TrustedOrigins"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (string.IsNullOrWhiteSpace(origin) || !trustedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ApiContractException(403, "invalid_guest_access", "Origin validation failed", "Guest checkout origin'i güvenilir değil.");
        }
    }
}

// Burada sepete ekleme HTTP gövdesinin istemciden kabul edilen sınırlı alanlarını tanımlıyorum.
public sealed record AddCartItemRequest(
    Guid ProductVariantId,
    int Quantity,
    Guid? ExpectedConcurrencyToken = null);

// Burada sepet satırı adet güncellemesinin HTTP gövdesini tanımlıyorum.
public sealed record UpdateCartItemQuantityRequest(
    int Quantity,
    Guid ExpectedConcurrencyToken);

// Burada guest checkout HTTP gövdesinin frontend tarafından gönderilebilen alanlarını tanımlıyorum.
public sealed record GuestCheckoutRequest(
    Guid ExpectedCartConcurrencyToken,
    GuestCustomerRequest Customer,
    GuestAddressRequest ShippingAddress,
    GuestAddressRequest? BillingAddress,
    Guid ShippingMethodId,
    string? CouponCode = null);

// Burada guest müşterinin zorunlu iletişim alanlarını tanımlıyorum.
public sealed record GuestCustomerRequest(string FirstName, string LastName, string Email, string PhoneNumber);

// Burada guest adresinde fiyat veya kullanıcı adres kimliği kabul etmeyen snapshot alanlarını tanımlıyorum.
public sealed record GuestAddressRequest(
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string FullAddress,
    string? PostalCode = null)
{
    // Burada HTTP adres modelini zorunlu shipping veya billing tipli Application girdisine dönüştürüyorum.
    public CheckoutAddressInput ToInput(AddressType type) => new(
        null, type, Title, FirstName, LastName, PhoneNumber, City, District, FullAddress, PostalCode);
}
