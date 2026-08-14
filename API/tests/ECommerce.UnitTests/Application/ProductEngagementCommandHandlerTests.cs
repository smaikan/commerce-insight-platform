using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Products.Engagement.Commands.AddFavorite;
using ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;
using ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;
using ECommerce.Application.Products.Engagement.Commands.UpsertRating;
using ECommerce.Application.Products.Engagement.Services;
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
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        ProductDailyMetric? createdMetric = null;
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == userId && owner.SessionId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FavoriteProduct?)null);
        engagement.Setup(item => item.GetProductDailyMetricForUpdateAsync(product.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDailyMetric?)null);
        engagement.Setup(item => item.AddProductDailyMetricAsync(It.IsAny<ProductDailyMetric>(), It.IsAny<CancellationToken>()))
            .Callback<ProductDailyMetric, CancellationToken>((metric, _) => createdMetric = metric)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new AddFavoriteCommandHandler(products.Object, engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(userId)), new FixedClock(), unitOfWork.Object);

        await handler.Handle(new AddFavoriteCommand(product.Id), CancellationToken.None);

        product.FavoriteCount.Should().Be(1);
        product.PopularityScore.Should().Be(Product.FavoriteScoreWeight);
        createdMetric.Should().NotBeNull();
        createdMetric!.FavoriteCount.Should().Be(1);
        engagement.Verify(item => item.AddFavoriteAsync(
            It.Is<FavoriteProduct>(favorite => favorite.ProductId == product.Id && favorite.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aynı ürünün ikinci kez favoriye eklenmesinin hiçbir sayaç veya kayıt değiştirmeden conflict ürettiğini doğruluyorum.
    [Fact]
    public async Task AddFavorite_Should_Reject_Duplicate_Without_Mutation()
    {
        const long userId = 1;
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == userId && owner.SessionId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FavoriteProduct(product.Id, userId));
        var handler = new AddFavoriteCommandHandler(
            products.Object,
            engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(userId)),
            new FixedClock(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new AddFavoriteCommand(product.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        product.FavoriteCount.Should().Be(0);
        product.PopularityScore.Should().Be(0);
        engagement.Verify(item => item.AddFavoriteAsync(
            It.IsAny<FavoriteProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada favoriden çıkarmanın toplam sayaç ve popülerlik puanını aynı kaydetme işleminde azalttığını doğruluyorum.
    [Fact]
    public async Task RemoveFavorite_Should_Update_Summary_And_Remove_Relation()
    {
        const long userId = 1;
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        product.IncreaseFavoriteCount();
        var favorite = new FavoriteProduct(product.Id, userId);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == userId && owner.SessionId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RemoveFavoriteCommandHandler(
            products.Object,
            engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(userId)),
            unitOfWork.Object);

        await handler.Handle(new RemoveFavoriteCommand(product.Id), CancellationToken.None);

        product.FavoriteCount.Should().Be(0);
        product.PopularityScore.Should().Be(0);
        engagement.Verify(item => item.RemoveFavorite(favorite), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada misafir favorisi eklemenin session sahipliğini ve sayaçları birlikte kaydettiğini doğruluyorum.
    [Fact]
    public async Task AddFavorite_Should_Update_Guest_Summary_And_Daily_Metric()
    {
        const string sessionId = "guest-session";
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var metric = new ProductDailyMetric(product.Id, new DateOnly(2026, 7, 14));
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == null && owner.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FavoriteProduct?)null);
        engagement.Setup(item => item.GetProductDailyMetricForUpdateAsync(
                product.Id,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new AddFavoriteCommandHandler(
            products.Object,
            engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(null)),
            new FixedClock(),
            unitOfWork.Object);

        await handler.Handle(new AddFavoriteCommand(product.Id, $"  {sessionId}  "), CancellationToken.None);

        product.FavoriteCount.Should().Be(1);
        product.PopularityScore.Should().Be(Product.FavoriteScoreWeight);
        metric.FavoriteCount.Should().Be(1);
        engagement.Verify(item => item.AddFavoriteAsync(
            It.Is<FavoriteProduct>(favorite =>
                favorite.ProductId == product.Id &&
                favorite.UserId == null &&
                favorite.SessionId == sessionId),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada misafir duplicate favorisinin hiçbir sayaç veya kayıt değiştirmeden conflict ürettiğini doğruluyorum.
    [Fact]
    public async Task AddFavorite_Should_Reject_Guest_Duplicate_Without_Mutation()
    {
        const string sessionId = "guest-session";
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == null && owner.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FavoriteProduct(product.Id, sessionId));
        var handler = new AddFavoriteCommandHandler(
            products.Object,
            engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(null)),
            new FixedClock(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new AddFavoriteCommand(product.Id, sessionId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        product.FavoriteCount.Should().Be(0);
        product.PopularityScore.Should().Be(0);
        engagement.Verify(item => item.AddFavoriteAsync(
            It.IsAny<FavoriteProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada misafir favorisini kaldırmanın session kaydını ve ürün sayaçlarını birlikte güncellediğini doğruluyorum.
    [Fact]
    public async Task RemoveFavorite_Should_Update_Guest_Summary_And_Remove_Relation()
    {
        const string sessionId = "guest-session";
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        product.IncreaseFavoriteCount();
        var favorite = new FavoriteProduct(product.Id, sessionId);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        products.Setup(item => item.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        engagement.Setup(item => item.GetFavoriteForUpdateAsync(
                product.Id,
                It.Is<FavoriteOwner>(owner => owner.UserId == null && owner.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RemoveFavoriteCommandHandler(
            products.Object,
            engagement.Object,
            new FavoriteOwnerResolver(new FixedCurrentUser(null)),
            unitOfWork.Object);

        await handler.Handle(
            new RemoveFavoriteCommand(product.Id, sessionId),
            CancellationToken.None);

        product.FavoriteCount.Should().Be(0);
        product.PopularityScore.Should().Be(0);
        engagement.Verify(item => item.RemoveFavorite(favorite), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada teslim edilmiş ürün puanının ürün ortalamasına doğru yansıtıldığını doğruluyorum.
    [Fact]
    public async Task UpsertRating_Should_Calculate_Product_Rating_Summary()
    {
        const long userId = 1;
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
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

    // Burada sepete ekleme metriğinin yalnız güvenilir Cart akışından kaydedilebilmesini sağlıyorum.
    [Fact]
    public async Task RecordActivity_Should_Reject_Direct_AddToCart()
    {
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new RecordProductActivityCommandHandler(
            products.Object,
            engagement.Object,
            new FixedClock(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new RecordProductActivityCommand(1, ProductActivityType.AddToCart, Guid.NewGuid(), 2),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        products.Verify(
            repository => repository.GetByIdForUpdateAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada favori transaction delegesini çalıştırıp kayıt sonucunu taklit eden unit of work hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedCurrentUser : ICurrentUserService
    {
        // Burada test için sabit kullanıcı kimliğini hazırlıyorum.
        public FixedCurrentUser(long? userId) => UserId = userId;
        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
    }
}
