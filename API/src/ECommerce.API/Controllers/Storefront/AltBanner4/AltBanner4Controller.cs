using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.AltBanner4;

[Route("api/alt-banner-4")]
public sealed class AltBanner4Controller : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.AltBanner4;

    // Burada dördüncü alt banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public AltBanner4Controller(ISender sender) : base(sender)
    {
    }
}
