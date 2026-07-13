using ECommerce.Application.Common.Services;
using FluentAssertions;

namespace ECommerce.UnitTests.Application;

public sealed class ProductUrlGeneratorTests
{
    [Theory]
    [InlineData("Oversize T-Shirt", "oversize-t-shirt")]
    [InlineData("Türkçe Şapkalı Ürün", "turkce-sapkali-urun")]
    [InlineData("  Sneaker   White  ", "sneaker-white")]
    public void Generate_Should_Return_Prepared_Product_Url(string title, string expectedUrl)
    {
        var generator = new ProductUrlGenerator();

        var url = generator.Generate(title);

        url.Should().Be(expectedUrl);
    }
}
