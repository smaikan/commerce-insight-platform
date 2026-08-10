using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class BrandImageTests
{
    // Burada marka görsel URL değerinin temizlenip kaldırılabildiğini doğruluyorum.
    [Fact]
    public void Brand_Should_Store_And_Clear_Optional_Image_Url()
    {
        var brand = new Brand(
            "Serantis",
            "serantis",
            imageUrl: " https://cdn.example.com/brands/serantis.jpg ");

        brand.ImageUrl.Should().Be("https://cdn.example.com/brands/serantis.jpg");

        brand.SetImageUrl(" ");

        brand.ImageUrl.Should().BeNull();
    }
}
