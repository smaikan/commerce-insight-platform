using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ProductPopularityScoreTests
{
    // Burada tüm etkileşimlerin belirlenen ağırlıklarla ürün puanına yansıdığını doğruluyorum.
    [Fact]
    public void Product_Activities_Should_Increase_Popularity_Score_With_Defined_Weights()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN");

        product.IncreaseClickCount();
        product.IncreaseFavoriteCount();
        product.IncreaseTotalAddToCartCount(2);
        product.IncreaseTotalPurchaseCount(3);

        product.PopularityScore.Should().Be(81);
        product.ClickCount.Should().Be(1);
        product.FavoriteCount.Should().Be(1);
        product.TotalAddToCartCount.Should().Be(2);
        product.TotalPurchaseCount.Should().Be(3);
    }

    // Burada favoriden çıkarma işleminin daha önce eklenen favori puanını geri aldığını doğruluyorum.
    [Fact]
    public void Removing_Favorite_Should_Decrease_Popularity_Score()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN");
        product.IncreaseFavoriteCount();

        product.DecreaseFavoriteCount();

        product.PopularityScore.Should().Be(0);
        product.FavoriteCount.Should().Be(0);
    }
}
