using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Products.Engagement.Commands.CreateReview;
using ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;
using ECommerce.Application.Products.Engagement.Commands.UpsertRating;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;
using ECommerce.Application.Users.Commands.SetUserRole;
using ECommerce.Application.Users.Commands.SetUserStatus;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class RequestedBusinessRuleTests
{
    // Burada en küçük int değerinin stok güncellemesinde validasyondan geçmediğini doğruluyorum.
    [Fact]
    public void StockValidator_Should_Reject_IntMinValue()
    {
        var validator = new UpdateProductVariantStockCommandValidator();

        var result = validator.TestValidate(new UpdateProductVariantStockCommand(
            Guid.NewGuid(), int.MinValue, "Invalid adjustment"));

        result.ShouldHaveValidationErrorFor(command => command.Quantity);
    }

    // Burada stok toplamının int sınırını aşarak negatife dönmesini engellediğimi doğruluyorum.
    [Fact]
    public void ProductVariant_Should_Reject_Stock_Overflow()
    {
        var variant = new ProductVariant(1, "Standard", "SKU-MAX", 100m, int.MaxValue);

        var act = () => variant.IncreaseStock(1);

        act.Should().Throw<DomainException>()
            .WithMessage("Stock cannot exceed the maximum supported value.");
    }

    // Burada stok handlerının doğrudan çağrıldığında da en küçük int değeriyle 500 üretmediğini doğruluyorum.
    [Fact]
    public async Task StockHandler_Should_Reject_IntMinValue_Before_MathAbs()
    {
        var handler = new UpdateProductVariantStockCommandHandler(
            Mock.Of<IProductVariantRepository>(),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(
            new UpdateProductVariantStockCommand(Guid.NewGuid(), int.MinValue, "Invalid adjustment"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Quantity is outside the supported range.");
    }

    // Burada ürün ve varyant sayaçlarının int sınırını aşınca bozulmadan long olarak devam ettiğini doğruluyorum.
    [Fact]
    public void EngagementCounters_Should_Continue_Beyond_IntMaxValue()
    {
        var product = new Product("Product", "product").WithId(10);
        var variant = new ProductVariant(product, "Standard", "SKU-LONG", 100m, 1);

        product.IncreaseTotalAddToCartCount(int.MaxValue);
        product.IncreaseTotalAddToCartCount(1);
        variant.IncreaseAddToCartCount(int.MaxValue);
        variant.IncreaseAddToCartCount(1);

        product.TotalAddToCartCount.Should().Be((long)int.MaxValue + 1);
        variant.AddToCartCount.Should().Be((long)int.MaxValue + 1);
    }

    // Burada teslim edilmemiş ürünün puanlanamadığını doğruluyorum.
    [Fact]
    public async Task UpsertRating_Should_Reject_Product_Without_Delivered_Order()
    {
        const long userId = 1;
        var product = new Product("Product", "product").WithId(10);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        products.Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        engagement.Setup(repository => repository.HasDeliveredPurchaseAsync(product.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new UpsertRatingCommandHandler(
            products.Object,
            engagement.Object,
            new FixedCurrentUser(userId),
            new FixedClock(),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(new UpsertRatingCommand(product.Id, 5), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("The product can only be rated after it has been delivered.");
    }

    // Burada teslim edilmemiş ürün için yorum oluşturulamadığını doğruluyorum.
    [Fact]
    public async Task CreateReview_Should_Reject_Product_Without_Delivered_Order()
    {
        const long userId = 1;
        var product = new Product("Product", "product").WithId(10);
        var products = new Mock<IProductRepository>();
        var engagement = new Mock<IProductEngagementRepository>();
        products.Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        engagement.Setup(repository => repository.HasDeliveredPurchaseAsync(product.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new CreateReviewCommandHandler(
            products.Object,
            engagement.Object,
            new FixedCurrentUser(userId),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(
            new CreateReviewCommand(product.Id, "Comment", "Title", 5),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("The product can only be reviewed after it has been delivered.");
    }

    // Burada müşterinin tıklama ve sepete ekleme dışındaki hareketleri gönderemediğini doğruluyorum.
    [Fact]
    public void CustomerActivityValidator_Should_Reject_Purchase_Claims()
    {
        var validator = new RecordProductActivityCommandValidator();

        var result = validator.TestValidate(new RecordProductActivityCommand(
            1, ProductActivityType.Purchase, Guid.NewGuid(), 1));

        result.ShouldHaveValidationErrorFor(command => command.ActivityType);
    }

    // Burada son adminin kendi rolünü düşüremediğini doğruluyorum.
    [Fact]
    public async Task SetUserRole_Should_Protect_Last_Admin()
    {
        var admin = new User("admin@example.com", "hash", "Admin", "User", role: UserRole.Admin).WithId(1);
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdForUpdateAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        users.Setup(repository => repository.HasAnotherActiveAdminAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new SetUserRoleCommandHandler(
            users.Object,
            new ImmediateUnitOfWork());

        var act = () => handler.Handle(
            new SetUserRoleCommand(admin.Id, UserRole.Customer),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("The last active admin role cannot be removed.");
        admin.Role.Should().Be(UserRole.Admin);
    }

    // Burada başka aktif admin varken bir adminin rolünün değiştirilebildiğini doğruluyorum.
    [Fact]
    public async Task SetUserRole_Should_Allow_Change_When_Another_Active_Admin_Exists()
    {
        var admin = new User("admin@example.com", "hash", "Admin", "User", role: UserRole.Admin).WithId(1);
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdForUpdateAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        users.Setup(repository => repository.HasAnotherActiveAdminAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new SetUserRoleCommandHandler(users.Object, new ImmediateUnitOfWork());

        await handler.Handle(new SetUserRoleCommand(admin.Id, UserRole.Customer), CancellationToken.None);

        admin.Role.Should().Be(UserRole.Customer);
    }

    // Burada son admin hesabının pasife alınamadığını doğruluyorum.
    [Fact]
    public async Task SetUserStatus_Should_Protect_Last_Admin()
    {
        var admin = new User("admin@example.com", "hash", "Admin", "User", role: UserRole.Admin).WithId(1);
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdForUpdateAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        users.Setup(repository => repository.HasAnotherActiveAdminAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new SetUserStatusCommandHandler(
            users.Object,
            new FixedClock(),
            new ImmediateUnitOfWork());

        var act = () => handler.Handle(
            new SetUserStatusCommand(admin.Id, UserStatus.Passive),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("The last active admin account cannot be deactivated or deleted.");
        admin.Status.Should().Be(UserStatus.Active);
    }

    private sealed class FixedCurrentUser : ICurrentUserService
    {
        // Burada test için sabit kullanıcı kimliğini hazırlıyorum.
        public FixedCurrentUser(long userId) => UserId = userId;

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        // Burada testteki değişikliklerin kaydedildiğini temsil ediyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        // Burada production transaction sınırını test sırasında aynı çağrı içinde çalıştırıyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
