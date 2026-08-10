using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.AltBanner2;

[Route("api/alt-banner-2")]
public sealed class AltBanner2Controller : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.AltBanner2;

    // Burada ikinci alt banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public AltBanner2Controller(ISender sender) : base(sender)
    {
    }
}
