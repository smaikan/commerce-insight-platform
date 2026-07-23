using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Products.Engagement.Commands.AddFavorite;
using ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;
using ECommerce.Application.Products.Engagement.Commands.UpsertRating;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductEngagementCommandHandlerTests
{
    // Burada favori eklemenin ürün ve günlük sayaçlarını birlikte artırdığını doğruluyorum.
    [Fact]
    public async Task AddFavorite_Should_Update_Summary_And_Daily_Metric()
    {
        const long userId = 1;
        var product = new Product("Product", "product", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        ProductDailyMetric? createdMetric = null;
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(product.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FavoriteProduct?)null);
        engagement.Setup(item => item.GetProductDailyMetricForUpdateAsync(product.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDailyMetric?)null);
        engagement.Setup(item => item.AddProductDailyMetricAsync(It.IsAny<ProductDailyMetric>(), It.IsAny<CancellationToken>()))
            .Callback<ProductDailyMetric, CancellationToken>((metric, _) => createdMetric = metric)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new AddFavoriteCommandHandler(products.Object, engagement.Object,
            new FixedCurrentUser(userId), new FixedClock(), unitOfWork.Object);

        await handler.Handle(new AddFavoriteCommand(product.Id), CancellationToken.None);

        product.FavoriteCount.Should().Be(1);
        createdMetric.Should().NotBeNull();
        createdMetric!.FavoriteCount.Should().Be(1);
    }

    // Burada teslim edilmiş ürün puanının ürün ortalamasına doğru yansıtıldığını doğruluyorum.
    [Fact]
    public async Task UpsertRating_Should_Calculate_Product_Rating_Summary()
    {
        const long userId = 1;
        var product = new Product("Product", "product", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        engagement.Setup(item => item.GetRatingForUpdateAsync(product.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductRating?)null);
        engagement.Setup(item => item.HasDeliveredPurchaseAsync(product.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        engagement.Setup(item => item.GetRatingAggregateAsync(product.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((8m, 2L));
        engagement.Setup(item => item.GetProductDailyMetricForUpdateAsync(product.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDailyMetric(product.Id, DateOnly.FromDateTime(DateTime.UtcNow)));
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new UpsertRatingCommandHandler(products.Object, engagement.Object,
            new FixedCurrentUser(userId), new FixedClock(), unitOfWork.Object);

        await handler.Handle(new UpsertRatingCommand(product.Id, 5), CancellationToken.None);

        product.RatingCount.Should().Be(3);
        product.AverageRating.Should().BeApproximately(4.33m, 0.01m);
    }

    // Burada müşterinin sepete ekleme hareketinin ürün, varyant ve günlük sayaçlara birlikte işlendiğini doğruluyorum.
    [Fact]
    public async Task RecordActivity_Should_Record_Customer_AddToCart()
    {
        var product = new Product("Product", "product").WithId(1);
        var variant = new ProductVariant(product, "Standard", "SKU-CART", 100m, 10);
        var productMetric = new ProductDailyMetric(product.Id, new DateOnly(2026, 7, 14));
        var variantMetric = new ProductVariantDailyMetric(variant.Id, new DateOnly(2026, 7, 14));
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        variants.Setup(item => item.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(variant);
        engagement.Setup(item => item.GetProductDailyMetricForUpdateAsync(product.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productMetric);
        engagement.Setup(item => item.GetVariantDailyMetricForUpdateAsync(variant.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(variantMetric);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RecordProductActivityCommandHandler(
            products.Object, variants.Object, engagement.Object, new FixedClock(), unitOfWork.Object);

        await handler.Handle(
            new RecordProductActivityCommand(product.Id, ProductActivityType.AddToCart, variant.Id, 2),
            CancellationToken.None);

        product.TotalAddToCartCount.Should().Be(2);
        variant.AddToCartCount.Should().Be(2);
        productMetric.AddToCartCount.Should().Be(2);
        variantMetric.AddToCartCount.Should().Be(2);
        product.PopularityScore.Should().Be(16);
    }

    private sealed class FixedCurrentUser : ICurrentUserService
    {
        // Burada test için sabit kullanıcı kimliğini hazırlıyorum.
        public FixedCurrentUser(long userId) => UserId = userId;
        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
    }
}
