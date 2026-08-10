using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Storefront.MainBanners;

[Route("api/main-banners")]
public sealed class MainBannersController : BannerSectionControllerBase
{
    protected override StorefrontBannerSection Section => StorefrontBannerSection.Main;

    // Burada ana banner endpointini ortak bölüm davranışıyla hazırlıyorum.
    public MainBannersController(ISender sender) : base(sender)
    {
    }
}
