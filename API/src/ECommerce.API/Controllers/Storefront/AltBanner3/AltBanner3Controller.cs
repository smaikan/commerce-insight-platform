using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.AltBanner3;

[Route("api/alt-banner-3")]
public sealed class AltBanner3Controller : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.AltBanner3;

    // Burada üçüncü alt banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public AltBanner3Controller(ISender sender) : base(sender)
    {
    }
}
