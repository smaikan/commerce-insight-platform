using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.MainMobileBanner;

[Route("api/main-banner-mobile")]
public sealed class MainMobileBannerController : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.MainMobile;

    // Burada mobil ana banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public MainMobileBannerController(ISender sender) : base(sender)
    {
    }
}
