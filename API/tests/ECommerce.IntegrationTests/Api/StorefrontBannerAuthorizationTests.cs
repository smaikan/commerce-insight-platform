using ECommerce.API.Controllers.Storefront;
using ECommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.IntegrationTests.Api;

public sealed class StorefrontBannerAuthorizationTests
{
    // Burada banner okumasının anonim, güncellemesinin ise yalnız Admin politikasına açık olduğunu doğruluyorum.
    [Fact]
    public void Banner_Endpoints_Should_Expose_Public_Read_And_Admin_Write()
    {
        var getMethod = typeof(StorefrontBannersController)
            .GetMethod(nameof(StorefrontBannersController.Get))!;
        var updateMethod = typeof(StorefrontBannersController)
            .GetMethod(nameof(StorefrontBannersController.Update))!;

        getMethod.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Should().ContainSingle();
        updateMethod.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Policy == AuthorizationPolicies.AdminOnly);
    }
}
