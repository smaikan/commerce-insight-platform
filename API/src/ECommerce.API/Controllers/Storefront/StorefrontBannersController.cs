using ECommerce.API.Security;
using ECommerce.Application.StorefrontBanners.Commands.UpdateStorefrontBanners;
using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Application.StorefrontBanners.Queries.GetStorefrontBanners;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront;

[ApiController]
[Route("api/storefront-banners")]
public sealed class StorefrontBannersController : ControllerBase
{
    private readonly ISender _sender;

    // Burada banner HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public StorefrontBannersController(ISender sender)
    {
        _sender = sender;
    }

    // Burada storefront için ana ve alt banner URL'lerini herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<StorefrontBannersDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetStorefrontBannersQuery(), cancellationToken));
    }

    // Burada yalnız yöneticinin tüm banner setini atomik olarak değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut]
    public async Task<ActionResult<StorefrontBannersDto>> Update(
        StorefrontBannersRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new UpdateStorefrontBannersCommand(
            request.MainBannerImageUrl,
            request.AltBannerImageUrls), cancellationToken));
    }
}

// Burada storefront banner güncelleme HTTP gövdesini tanımlıyorum.
public sealed record StorefrontBannersRequest(
    string? MainBannerImageUrl = null,
    IReadOnlyList<string>? AltBannerImageUrls = null);
