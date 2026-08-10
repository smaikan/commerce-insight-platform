using ECommerce.API.Controllers.Product;
using ECommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class ProductFilteringAuthorizationTests
{
    // Burada admin ürün listesinin AdminOnly politikasını koruduğunu doğruluyorum.
    [Fact]
    public void Admin_Product_List_Should_Require_AdminOnly_Policy()
    {
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.GetList));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Policy == AuthorizationPolicies.AdminOnly);
    }

    // Burada ayrı storefront sınıflandırma rotalarının anonim erişime açık ve doğru şablonda olduğunu doğruluyorum.
    [Theory]
    [InlineData(nameof(ProductsController.GetPublishedByCollection), "by-collection/{collectionId:guid}")]
    [InlineData(nameof(ProductsController.GetPublishedByTag), "by-tag/{tagId:guid}")]
    [InlineData(nameof(ProductsController.GetPublishedByType), "by-type/{typeId:guid}")]
    [InlineData(nameof(ProductsController.GetPublishedByBrand), "by-brand/{brandId:guid}")]
    public void Storefront_Taxonomy_Routes_Should_Be_Anonymous(string methodName, string route)
    {
        var method = typeof(ProductsController).GetMethod(methodName);

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Should().ContainSingle();
        method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>()
            .Should().ContainSingle(attribute => attribute.Template == route);
    }
}
