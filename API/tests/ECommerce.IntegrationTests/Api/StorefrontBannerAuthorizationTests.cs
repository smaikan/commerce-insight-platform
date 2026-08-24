using ECommerce.API.Controllers.Storefront.AltBanner1;
using ECommerce.API.Controllers.Storefront.AltBanner2;
using ECommerce.API.Controllers.Storefront.AltBanner3;
using ECommerce.API.Controllers.Storefront.AltBanner4;
using ECommerce.API.Controllers.Storefront.AltBanner5;
using ECommerce.API.Controllers.Storefront.BannerSections;
using ECommerce.API.Controllers.Storefront.MainBanners;
using ECommerce.API.Controllers.Storefront.MainMobileBanner;
using ECommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class StorefrontBannerAuthorizationTests
{
    // Burada tüm banner bölümlerinin ayrı route kullandığını, public okuma ve Admin yazma sınırını doğruluyorum.
    [Theory]
    [InlineData(typeof(MainBannersController), "api/main-banners")]
    [InlineData(typeof(MainMobileBannerController), "api/main-banner-mobile")]
    [InlineData(typeof(AltBanner1Controller), "api/alt-banner-1")]
    [InlineData(typeof(AltBanner2Controller), "api/alt-banner-2")]
    [InlineData(typeof(AltBanner3Controller), "api/alt-banner-3")]
    [InlineData(typeof(AltBanner4Controller), "api/alt-banner-4")]
    [InlineData(typeof(AltBanner5Controller), "api/alt-banner-5")]
    public void Banner_Endpoints_Should_Use_Separate_Routes_With_Public_Read_And_Admin_Management(
        Type controllerType,
        string route)
    {
        controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Should().ContainSingle(attribute => attribute.Template == route);

        var getMethod = controllerType.GetMethod(nameof(BannerSectionControllerBase.Get))!;
        var getAdminMethod = controllerType.GetMethod(nameof(BannerSectionControllerBase.GetAdmin))!;
        var replaceMethod = controllerType.GetMethod(nameof(BannerSectionControllerBase.Replace))!;

        getMethod.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Should().ContainSingle();
        AssertAdminOnly(getAdminMethod);
        AssertAdminOnly(replaceMethod);
    }

    // Burada yönetim metodunun AdminOnly politikasıyla korunduğunu ortak biçimde doğruluyorum.
    private static void AssertAdminOnly(System.Reflection.MethodInfo method)
    {
        method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Policy == AuthorizationPolicies.AdminOnly);
    }
}
