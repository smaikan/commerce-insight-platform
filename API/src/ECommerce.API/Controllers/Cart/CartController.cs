using System.Security.Cryptography;
using ECommerce.Application.Carts.Commands.AddCartItem;
using ECommerce.Application.Carts.Commands.ClearCart;
using ECommerce.Application.Carts.Commands.MergeGuestCart;
using ECommerce.Application.Carts.Commands.RemoveCartItem;
using ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Queries.GetCart;
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
    private const string GuestCartCookieName = "ecommerce_guest_cart";
    private const string GuestCartCookiePath = "/api/cart";
    private const int GuestCartTokenByteLength = 32;
    private static readonly TimeSpan GuestCartCookieLifetime = TimeSpan.FromDays(30);
    private readonly ISender _sender;

    // Burada Cart HTTP isteklerini Application akışlarına yönlendirecek sender'ı hazırlıyorum.
    public CartController(ISender sender)
    {
        _sender = sender;
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

    // Burada giriş yapmış kullanıcıda cookie'yi yok sayıp anonim istekte güvenli guest session üretiyorum veya okuyorum.
    private string? GetSessionIdForCartAccess()
    {
        return User.Identity?.IsAuthenticated == true
            ? null
            : GetOrCreateGuestCartSessionId();
    }

    // Burada yalnız geçerli biçimdeki daha önce verilmiş misafir sepet oturumunu okuyorum.
    private string? GetExistingGuestCartSessionId()
    {
        return Request.Cookies.TryGetValue(GuestCartCookieName, out var sessionId) &&
               IsCanonicalGuestCartSessionId(sessionId)
            ? sessionId
            : null;
    }

    // Burada eksik veya bozuk cookie yerine kriptografik olarak rastgele yeni bir misafir sepet oturumu yazıyorum.
    private string GetOrCreateGuestCartSessionId()
    {
        var existingSessionId = GetExistingGuestCartSessionId();
        if (existingSessionId is not null)
        {
            return existingSessionId;
        }

        var sessionId = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(GuestCartTokenByteLength));
        Response.Cookies.Append(GuestCartCookieName, sessionId, CreateGuestCartCookieOptions());
        return sessionId;
    }

    // Burada cookie değeri için yalnız sunucunun ürettiği 256 bitlik büyük harfli hexadecimal biçimi kabul ediyorum.
    private static bool IsCanonicalGuestCartSessionId(string sessionId)
    {
        return sessionId.Length == GuestCartTokenByteLength * 2 &&
               sessionId.All(character =>
                   character is >= '0' and <= '9' or >= 'A' and <= 'F');
    }

    // Burada misafir sepet cookie'sini tarayıcı betiklerinden ve düz HTTP'den koruyan seçenekleri oluşturuyorum.
    private static CookieOptions CreateGuestCartCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = GuestCartCookiePath,
            MaxAge = GuestCartCookieLifetime,
            Expires = DateTimeOffset.UtcNow.Add(GuestCartCookieLifetime)
        };
    }

    // Burada misafir sepeti kullanıcıya devredildiğinde artık geçersiz olan browser cookie'sini siliyorum.
    private void DeleteGuestCartCookie()
    {
        Response.Cookies.Delete(GuestCartCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = GuestCartCookiePath
        });
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
