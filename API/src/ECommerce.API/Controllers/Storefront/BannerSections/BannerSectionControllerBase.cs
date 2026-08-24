using ECommerce.API.Security;
using ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;
using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Application.StorefrontBanners.Queries.GetBannerSection;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.BannerSections;

[ApiController]
public abstract class BannerSectionControllerBase : ControllerBase
{
    private readonly ISender _sender;

    protected abstract StorefrontBannerSection Section { get; }

    // Burada bağımsız banner bölümünün HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    protected BannerSectionControllerBase(ISender sender)
    {
        _sender = sender;
    }

    // Burada yalnız aktif banner kayıtlarını storefront kullanımı için anonim olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<BannerSectionDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetBannerSectionQuery(Section, ActiveOnly: true), cancellationToken));
    }

    // Burada bölümün aktif ve pasif tüm kayıtlarını yalnız yöneticiye sunuyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpGet("admin")]
    public async Task<ActionResult<BannerSectionDto>> GetAdmin(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetBannerSectionQuery(Section, ActiveOnly: false), cancellationToken));
    }

    // Burada yöneticinin tek banner bölümünü diğer bölümlere dokunmadan atomik değiştirmesine izin veriyor ve Storefront cache'ini temizliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut]
    public async Task<ActionResult<BannerSectionDto>> Replace(
        BannerSectionRequest request,
        [FromServices] ECommerce.Application.Common.Interfaces.IStorefrontRevalidationService revalidationService,
        CancellationToken cancellationToken)
    {
        var items = (request.Items ?? [])
            .Select(item => new BannerItemInput(
                item.Name,
                item.Key,
                item.MediaUrl,
                item.MediaType,
                item.TargetUrl,
                item.AltText,
                item.DisplayOrder,
                item.IsActive,
                item.IsMain))
            .ToList();
        var result = await _sender.Send(new ReplaceBannerSectionCommand(Section, items), cancellationToken);
        await revalidationService.RevalidateBannersAsync(CancellationToken.None);
        return Ok(result);
    }
}

// Burada bir banner bölümünün en fazla beş medya kaydını taşıyan HTTP gövdesini tanımlıyorum.
public sealed record BannerSectionRequest(IReadOnlyList<BannerItemRequest>? Items = null);

// Burada banner medyasının yönetilebilir HTTP alanlarını tanımlıyorum.
public sealed record BannerItemRequest(
    string Name,
    string Key,
    string MediaUrl,
    BannerMediaType MediaType,
    string? TargetUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    bool IsMain = false);
