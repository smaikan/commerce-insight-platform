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

    [Fact]
    public void ReplacePerformanceMetrics_Should_Replace_All_Product_Metrics_And_Recalculate_Popularity()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN");

        product.ReplacePerformanceMetrics(10, 5, 3, 2, 4.25m, 8, 6);

        product.ClickCount.Should().Be(10);
        product.TotalAddToCartCount.Should().Be(5);
        product.TotalPurchaseCount.Should().Be(3);
        product.FavoriteCount.Should().Be(2);
        product.AverageRating.Should().Be(4.25m);
        product.RatingCount.Should().Be(8);
        product.ReviewCount.Should().Be(6);
        product.PopularityScore.Should().Be(118);
    }

    [Fact]
    public void ReplacePerformanceMetrics_Should_Reject_Average_When_No_Ratings_Exist()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN");

        Action act = () => product.ReplacePerformanceMetrics(0, 0, 0, 0, 1m, 0, 0);

        act.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }
}
