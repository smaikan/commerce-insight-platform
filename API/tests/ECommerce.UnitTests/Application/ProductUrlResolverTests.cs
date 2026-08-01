using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductUrlResolverTests
{
    [Fact]
    public async Task ResolveAsync_Should_Add_Numeric_Suffix_For_A_Generated_Collision()
    {
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(item => item.UrlExistsAsync("urun", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resolver = new ProductUrlResolver(repository.Object, new ProductUrlGenerator());

        var url = await resolver.ResolveAsync("Ürün", null);

        url.Should().Be("urun-2");
    }

    [Fact]
    public async Task ResolveAsync_Should_Reject_An_Explicit_Reserved_Url()
    {
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(item => item.ReservedUrlExistsAsync("eski-urun", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resolver = new ProductUrlResolver(repository.Object, new ProductUrlGenerator());

        Func<Task> act = () => resolver.ResolveAsync("Yeni Ürün", "eski-urun");

        await act.Should().ThrowAsync<ConflictException>();
    }
}
