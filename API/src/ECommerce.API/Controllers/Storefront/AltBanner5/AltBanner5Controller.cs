using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.AltBanner5;

[Route("api/alt-banner-5")]
public sealed class AltBanner5Controller : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.AltBanner5;

    // Burada beşinci alt banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public AltBanner5Controller(ISender sender) : base(sender)
    {
    }
}
