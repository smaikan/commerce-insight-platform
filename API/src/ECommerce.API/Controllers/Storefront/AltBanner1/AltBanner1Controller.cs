using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.AltBanner1;

[Route("api/alt-banner-1")]
public sealed class AltBanner1Controller : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.AltBanner1;

    // Burada birinci alt banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public AltBanner1Controller(ISender sender) : base(sender)
    {
    }
}
