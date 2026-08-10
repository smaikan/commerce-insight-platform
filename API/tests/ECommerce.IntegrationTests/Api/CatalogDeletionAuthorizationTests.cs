using System.Reflection;
using ECommerce.API.Controllers.Product;
using ECommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class CatalogDeletionAuthorizationTests
{
    // Burada bütün katalog silme uçlarının DELETE rotası, AdminOnly politikası ve 204 yanıt sözleşmesini doğruluyorum.
    [Theory]
    [InlineData(typeof(ProductsController), nameof(ProductsController.Delete), "{id}")]
    [InlineData(typeof(ProductImagesController), nameof(ProductImagesController.Delete), "{id:guid}")]
    [InlineData(typeof(BrandsController), nameof(BrandsController.Delete), "{id:guid}")]
    [InlineData(typeof(CollectionsController), nameof(CollectionsController.Delete), "{id:guid}")]
    [InlineData(typeof(ProductTypesController), nameof(ProductTypesController.Delete), "{id:guid}")]
    [InlineData(typeof(TagsController), nameof(TagsController.Delete), "{id:guid}")]
    public void Delete_Endpoints_Should_Require_AdminOnly_And_Return_NoContent(
        Type controllerType,
        string methodName,
        string route)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        method.Should().NotBeNull();
        method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().ContainSingle(attribute => attribute.Policy == AuthorizationPolicies.AdminOnly);
        method.GetCustomAttributes<HttpDeleteAttribute>(inherit: true)
            .Should().ContainSingle(attribute => attribute.Template == route);
        method.GetCustomAttributes<ProducesResponseTypeAttribute>(inherit: true)
            .Should().ContainSingle(attribute => attribute.StatusCode == StatusCodes.Status204NoContent);
    }
}
