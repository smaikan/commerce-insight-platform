using ECommerce.Application.Carts.Commands.AddCartItem;
using ECommerce.Application.Carts.Commands.ClearCart;
using ECommerce.Application.Carts.Commands.MergeGuestCart;
using ECommerce.Application.Carts.Commands.RemoveCartItem;
using ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Queries.GetCart;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CartApplicationTests
{
    // Burada giriş yapmış kullanıcının gönderilen misafir oturumundan bağımsız olarak sepet sahibi seçildiğini doğruluyorum.
    [Fact]
    public void OwnerResolver_Should_Prioritize_Authenticated_User()
    {
        var resolver = new CartOwnerResolver(new StubCurrentUser(42));

        var owner = resolver.Resolve("spoofed-guest-session");

        owner.UserId.Should().Be(42);
        owner.SessionId.Should().BeNull();
        owner.IsGuest.Should().BeFalse();
    }

    // Burada anonim isteğin temizlenmiş geçerli oturumla misafir sepet sahibi oluşturduğunu doğruluyorum.
    [Fact]
    public void OwnerResolver_Should_Normalize_Guest_Session()
    {
        var resolver = new CartOwnerResolver(new StubCurrentUser(null));

        var owner = resolver.Resolve("  guest-session  ");

        owner.UserId.Should().BeNull();
        owner.SessionId.Should().Be("guest-session");
        owner.IsGuest.Should().BeTrue();
    }

    // Burada anonim isteğin geçerli misafir oturumu olmadan sepet erişimi kazanamadığını doğruluyorum.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OwnerResolver_Should_Reject_Missing_Guest_Session(string? sessionId)
    {
        var resolver = new CartOwnerResolver(new StubCurrentUser(null));

        Action act = () => resolver.Resolve(sessionId);

        act.Should().Throw<UnauthorizedException>();
    }

    // Burada tüm Cart validatorlarının geçersiz kimlik, adet, token ve session alanlarını reddettiğini doğruluyorum.
    [Fact]
    public void CartValidators_Should_Reject_Invalid_Requests()
    {
        var tooLongSession = new string('x', Cart.MaximumSessionIdLength + 1);

        new AddCartItemCommandValidator()
            .Validate(new AddCartItemCommand(Guid.Empty, 0, tooLongSession, Guid.Empty))
            .IsValid.Should().BeFalse();
        new UpdateCartItemQuantityCommandValidator()
            .Validate(new UpdateCartItemQuantityCommand(Guid.Empty, 0, Guid.Empty, tooLongSession))
            .IsValid.Should().BeFalse();
        new RemoveCartItemCommandValidator()
            .Validate(new RemoveCartItemCommand(Guid.Empty, Guid.Empty, tooLongSession))
            .IsValid.Should().BeFalse();
        new ClearCartCommandValidator()
            .Validate(new ClearCartCommand(Guid.Empty, tooLongSession))
            .IsValid.Should().BeFalse();
        new MergeGuestCartCommandValidator()
            .Validate(new MergeGuestCartCommand(string.Empty))
            .IsValid.Should().BeFalse();
        new GetCartQueryValidator()
            .Validate(new GetCartQuery(tooLongSession))
            .IsValid.Should().BeFalse();
    }

    // Burada tüm Cart validatorlarının sınırlar içindeki geçerli istekleri kabul ettiğini doğruluyorum.
    [Fact]
    public void CartValidators_Should_Accept_Valid_Requests()
    {
        var itemId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var token = Guid.NewGuid();

        new AddCartItemCommandValidator()
            .Validate(new AddCartItemCommand(variantId, 1, "guest", token))
            .IsValid.Should().BeTrue();
        new UpdateCartItemQuantityCommandValidator()
            .Validate(new UpdateCartItemQuantityCommand(itemId, 2, token, "guest"))
            .IsValid.Should().BeTrue();
        new RemoveCartItemCommandValidator()
            .Validate(new RemoveCartItemCommand(itemId, token, "guest"))
            .IsValid.Should().BeTrue();
        new ClearCartCommandValidator()
            .Validate(new ClearCartCommand(token, "guest"))
            .IsValid.Should().BeTrue();
        new MergeGuestCartCommandValidator()
            .Validate(new MergeGuestCartCommand("guest"))
            .IsValid.Should().BeTrue();
        new GetCartQueryValidator()
            .Validate(new GetCartQuery("guest"))
            .IsValid.Should().BeTrue();
    }

    // Burada kalıcı sepeti olmayan sahibin yazma yapmadan boş sepet görünümü aldığını doğruluyorum.
    [Fact]
    public async Task GetCart_Should_Return_Empty_Dto_When_Cart_Does_Not_Exist()
    {
        var carts = new Mock<ICartRepository>();
        carts.Setup(repository => repository.GetByOwnerAsync(
                It.Is<CartOwner>(owner => owner.UserId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        var handler = new GetCartQueryHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)));

        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        result.Id.Should().BeNull();
        result.Items.Should().BeEmpty();
        result.TotalQuantity.Should().Be(0);
        result.SubTotal.Should().Be(0m);
    }

    // Burada mevcut sepetin güvenli public ürün kimliği, fiyat değişikliği ve kullanılabilirlik bilgileriyle maplendiğini doğruluyorum.
    [Fact]
    public async Task GetCart_Should_Map_Current_Catalog_State()
    {
        var state = CreateCartState(quantity: 2, storedPrice: 10m, currentPrice: 12m, stock: 5);
        var carts = new Mock<ICartRepository>();
        carts.Setup(repository => repository.GetByOwnerAsync(
                It.IsAny<CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(state.Cart);
        var handler = new GetCartQueryHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)));

        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.HasPriceChanges.Should().BeTrue();
        result.HasUnavailableItems.Should().BeFalse();
        result.Items[0].ProductId.Should().StartWith("P");
        result.Items[0].CurrentUnitPrice.Should().Be(12m);
        result.Items[0].PriceChanged.Should().BeTrue();
    }

    // Burada ilk misafir eklemesinin güvenilir varyant fiyatıyla sepet oluşturduğunu ve metriği aynı transactionda kaydettiğini doğruluyorum.
    [Fact]
    public async Task AddCartItem_Should_Create_Guest_Cart_With_Trusted_Price()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, price: 125m, stock: 10);
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var metrics = new Mock<ICartMetricsRecorder>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        Cart? createdCart = null;
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.IsGuest && owner.SessionId == "guest-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        carts.Setup(repository => repository.AddAsync(
                It.IsAny<Cart>(),
                It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, _) => createdCart = cart)
            .Returns(Task.CompletedTask);
        carts.Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        products.Setup(repository => repository.GetByIdForUpdateAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        variants.Setup(repository => repository.GetByIdForUpdateAsync(
                variant.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        var handler = new AddCartItemCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            new CartOwnerResolver(new StubCurrentUser(null)),
            metrics.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new AddCartItemCommand(variant.Id, 2, "guest-1"),
            CancellationToken.None);

        createdCart.Should().NotBeNull();
        createdCart!.SessionId.Should().Be("guest-1");
        createdCart.Items.Should().ContainSingle();
        createdCart.Items.Single().ProductId.Should().Be(product.Id);
        createdCart.Items.Single().UnitPrice.Should().Be(125m);
        result.SubTotal.Should().Be(250m);
        metrics.Verify(recorder => recorder.RecordAddedQuantityAsync(
            product,
            variant,
            2,
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada mevcut sepete tokensız yinelenen ekleme isteğinin miktarı iki kez artırmasını engelliyorum.
    [Fact]
    public async Task AddCartItem_Should_Require_Token_When_Cart_Already_Exists()
    {
        var state = CreateCartState();
        var dependencies = CreateAddHandlerDependencies(
            state.Product,
            state.Variant,
            state.Cart);

        Func<Task> act = () => dependencies.Handler.Handle(
            new AddCartItemCommand(state.Variant.Id, 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        state.Item.Quantity.Should().Be(2);
        dependencies.UnitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
        dependencies.Metrics.VerifyNoOtherCalls();
    }

    // Burada mevcut sepet için eski concurrency token gönderildiğinde state ve metriklerin değişmediğini doğruluyorum.
    [Fact]
    public async Task AddCartItem_Should_Reject_Stale_Concurrency_Token()
    {
        var state = CreateCartState();
        var dependencies = CreateAddHandlerDependencies(state.Product, state.Variant, state.Cart);
        var handler = dependencies.Handler;

        Func<Task> act = () => handler.Handle(
            new AddCartItemCommand(
                state.Variant.Id,
                1,
                ExpectedConcurrencyToken: Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        state.Cart.Items.Single().Quantity.Should().Be(2);
        dependencies.Metrics.Verify(recorder => recorder.RecordAddedQuantityAsync(
            It.IsAny<Product>(),
            It.IsAny<ProductVariant>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        dependencies.UnitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada sepette oluşacak toplam adet stok miktarını aşarsa ekleme ve sayaç güncellemesinin yapılmadığını doğruluyorum.
    [Fact]
    public async Task AddCartItem_Should_Reject_Quantity_Above_Stock()
    {
        var state = CreateCartState(quantity: 2, stock: 3);
        var dependencies = CreateAddHandlerDependencies(state.Product, state.Variant, state.Cart);

        Func<Task> act = () => dependencies.Handler.Handle(
            new AddCartItemCommand(
                state.Variant.Id,
                2,
                ExpectedConcurrencyToken: state.Cart.ConcurrencyToken),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        state.Cart.Items.Single().Quantity.Should().Be(2);
        dependencies.Metrics.Verify(recorder => recorder.RecordAddedQuantityAsync(
            It.IsAny<Product>(),
            It.IsAny<ProductVariant>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada satışa kapalı ürünün sepete eklenmeden önce reddedildiğini doğruluyorum.
    [Fact]
    public async Task AddCartItem_Should_Reject_Unavailable_Product()
    {
        var product = new Product(
                "Passive",
                "passive",
                "PASSIVE-MAIN",
                status: ProductStatus.Passive,
                isActive: false)
            .WithId(25);
        var variant = CreateVariant(product, stock: 5);
        var dependencies = CreateAddHandlerDependencies(product, variant, null);

        Func<Task> act = () => dependencies.Handler.Handle(
            new AddCartItemCommand(variant.Id, 1, SessionId: "guest"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        dependencies.Carts.Verify(repository => repository.AddAsync(
            It.IsAny<Cart>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada adet artışının güncel fiyatı kullandığını ve yalnız artış farkını metriğe yansıttığını doğruluyorum.
    [Fact]
    public async Task UpdateQuantity_Should_Use_Current_Price_And_Record_Positive_Delta()
    {
        var state = CreateCartState(quantity: 2, storedPrice: 10m, currentPrice: 15m, stock: 10);
        var carts = CreateTrackedCartRepository(state.Cart);
        var metrics = new Mock<ICartMetricsRecorder>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new UpdateCartItemQuantityCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            metrics.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateCartItemQuantityCommand(
                state.Item.Id,
                5,
                state.Cart.ConcurrencyToken),
            CancellationToken.None);

        state.Item.Quantity.Should().Be(5);
        state.Item.UnitPrice.Should().Be(15m);
        result.SubTotal.Should().Be(75m);
        metrics.Verify(recorder => recorder.RecordAddedQuantityAsync(
            state.Product,
            state.Variant,
            3,
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada adet azaltımının sepeti güncellediğini ancak lifetime ekleme metriğini geri almadığını doğruluyorum.
    [Fact]
    public async Task UpdateQuantity_Should_Not_Record_Metric_When_Quantity_Decreases()
    {
        var state = CreateCartState(quantity: 4, storedPrice: 10m, currentPrice: 11m, stock: 10);
        var carts = CreateTrackedCartRepository(state.Cart);
        var metrics = new Mock<ICartMetricsRecorder>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new UpdateCartItemQuantityCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            metrics.Object,
            unitOfWork.Object);

        await handler.Handle(
            new UpdateCartItemQuantityCommand(
                state.Item.Id,
                1,
                state.Cart.ConcurrencyToken),
            CancellationToken.None);

        state.Item.Quantity.Should().Be(1);
        state.Item.UnitPrice.Should().Be(11m);
        metrics.Verify(recorder => recorder.RecordAddedQuantityAsync(
            It.IsAny<Product>(),
            It.IsAny<ProductVariant>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada değerler aynı olsa bile eşzamanlı yazma denetimi için tokenın yenilenip kaydedildiğini doğruluyorum.
    [Fact]
    public async Task UpdateQuantity_Should_Save_For_Concurrency_When_Values_Are_Unchanged()
    {
        var state = CreateCartState(quantity: 2, storedPrice: 10m, currentPrice: 10m, stock: 10);
        var originalToken = state.Cart.ConcurrencyToken;
        var carts = CreateTrackedCartRepository(state.Cart);
        var metrics = new Mock<ICartMetricsRecorder>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new UpdateCartItemQuantityCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            metrics.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateCartItemQuantityCommand(
                state.Item.Id,
                2,
                state.Cart.ConcurrencyToken),
            CancellationToken.None);

        result.ConcurrencyToken.Should().Be(state.Cart.ConcurrencyToken);
        state.Cart.ConcurrencyToken.Should().NotBe(originalToken);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        metrics.VerifyNoOtherCalls();
    }

    // Burada başka sepete ait item kimliğiyle adet güncellenemediğini doğruluyorum.
    [Fact]
    public async Task UpdateQuantity_Should_Reject_Item_Not_Owned_By_Cart()
    {
        var state = CreateCartState();
        var carts = CreateTrackedCartRepository(state.Cart);
        var handler = new UpdateCartItemQuantityCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            Mock.Of<ICartMetricsRecorder>(),
            Mock.Of<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new UpdateCartItemQuantityCommand(
                Guid.NewGuid(),
                2,
                state.Cart.ConcurrencyToken),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Burada satır silme komutunun yalnız owner'a ait itemı kaldırıp değişikliği kaydettiğini doğruluyorum.
    [Fact]
    public async Task RemoveCartItem_Should_Remove_Owned_Item()
    {
        var state = CreateCartState();
        var carts = CreateTrackedCartRepository(state.Cart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new RemoveCartItemCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            unitOfWork.Object);

        var result = await handler.Handle(
            new RemoveCartItemCommand(
                state.Item.Id,
                state.Cart.ConcurrencyToken),
            CancellationToken.None);

        state.Cart.Items.Should().BeEmpty();
        result.Items.Should().BeEmpty();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada eski concurrency token ile satır silinmesinin engellendiğini doğruluyorum.
    [Fact]
    public async Task RemoveCartItem_Should_Reject_Stale_Token()
    {
        var state = CreateCartState();
        var carts = CreateTrackedCartRepository(state.Cart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new RemoveCartItemCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new RemoveCartItemCommand(state.Item.Id, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        state.Cart.Items.Should().ContainSingle();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada sepet temizleme komutunun tüm itemları kaldırıp güncel boş görünümü döndürdüğünü doğruluyorum.
    [Fact]
    public async Task ClearCart_Should_Remove_All_Items()
    {
        var state = CreateCartState();
        state.Cart.AddItem(
            state.Product.Id,
            Guid.NewGuid(),
            1,
            5m);
        var token = state.Cart.ConcurrencyToken;
        var carts = CreateTrackedCartRepository(state.Cart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new ClearCartCommandHandler(
            carts.Object,
            new CartOwnerResolver(new StubCurrentUser(7)),
            unitOfWork.Object);

        var result = await handler.Handle(
            new ClearCartCommand(token),
            CancellationToken.None);

        state.Cart.Items.Should().BeEmpty();
        result.TotalQuantity.Should().Be(0);
        result.SubTotal.Should().Be(0m);
    }

    // Burada giriş yapmamış kullanıcının guest sepet birleştirme işlemini başlatamadığını doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Require_Authenticated_User()
    {
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            Mock.Of<ICartRepository>(),
            new StubCurrentUser(null),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new MergeGuestCartCommand("guest"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        unitOfWork.Verify(unit => unit.ExecuteInSerializableTransactionAsync(
            It.IsAny<Func<CancellationToken, Task<CartDto>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada kullanıcı sepeti yokken misafir sepetinin aynı aggregate korunarak kullanıcıya devredildiğini doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Assign_Guest_Cart_When_User_Cart_Does_Not_Exist()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, price: 25m, stock: 10);
        var guestCart = Cart.CreateForGuest("guest");
        var guestItem = guestCart.AddItem(product.Id, variant.Id, 1, 20m);
        AttachCatalog(guestItem, product, variant);
        var carts = new Mock<ICartRepository>();
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.UserId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.SessionId == "guest"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestCart);
        carts.Setup(repository => repository.GetByIdAsync(
                guestCart.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            carts.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        var result = await handler.Handle(
            new MergeGuestCartCommand("guest"),
            CancellationToken.None);

        guestCart.UserId.Should().Be(7);
        guestCart.SessionId.Should().BeNull();
        guestItem.UnitPrice.Should().Be(25m);
        result.Id.Should().Be(guestCart.Id);
        carts.Verify(repository => repository.Remove(
            It.IsAny<Cart>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada kullanıcı sepeti olmasa bile stok dışındaki misafir sepetinin hesaba devredilmesini engelliyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Reject_Assignment_When_Stock_Is_Insufficient()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, price: 25m, stock: 1);
        var guestCart = Cart.CreateForGuest("guest");
        var guestItem = guestCart.AddItem(product.Id, variant.Id, 2, 20m);
        AttachCatalog(guestItem, product, variant);
        var carts = CreateMergeRepository(null, guestCart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            carts.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new MergeGuestCartCommand("guest"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        guestCart.UserId.Should().BeNull();
        guestCart.SessionId.Should().Be("guest");
        guestItem.UnitPrice.Should().Be(20m);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada mevcut kullanıcı ve guest sepetlerindeki aynı varyantın güncel fiyatla tek satırda birleştirildiğini doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Merge_Duplicate_Variant_And_Remove_Guest_Cart()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, price: 15m, stock: 10);
        var userCart = Cart.CreateForUser(7);
        var userItem = userCart.AddItem(product.Id, variant.Id, 1, 10m);
        AttachCatalog(userItem, product, variant);
        var guestCart = Cart.CreateForGuest("guest");
        var guestItem = guestCart.AddItem(product.Id, variant.Id, 2, 12m);
        AttachCatalog(guestItem, product, variant);
        var carts = CreateMergeRepository(userCart, guestCart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            carts.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        var result = await handler.Handle(
            new MergeGuestCartCommand("guest"),
            CancellationToken.None);

        userCart.Items.Should().ContainSingle();
        userItem.Quantity.Should().Be(3);
        userItem.UnitPrice.Should().Be(15m);
        result.SubTotal.Should().Be(45m);
        carts.Verify(repository => repository.Remove(guestCart), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada merge toplamı stoğu aşarsa kullanıcı sepeti ve guest sepetin silinmeden kaldığını doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Reject_Combined_Quantity_Above_Stock()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, price: 15m, stock: 5);
        var userCart = Cart.CreateForUser(7);
        var userItem = userCart.AddItem(product.Id, variant.Id, 4, 10m);
        AttachCatalog(userItem, product, variant);
        var guestCart = Cart.CreateForGuest("guest");
        var guestItem = guestCart.AddItem(product.Id, variant.Id, 2, 12m);
        AttachCatalog(guestItem, product, variant);
        var carts = CreateMergeRepository(userCart, guestCart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            carts.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new MergeGuestCartCommand("guest"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        userItem.Quantity.Should().Be(4);
        carts.Verify(repository => repository.Remove(
            It.IsAny<Cart>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada misafir sepeti bulunmadığında mevcut kullanıcı sepetinin değişiklik kaydı olmadan döndürüldüğünü doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Return_User_Cart_When_Guest_Cart_Does_Not_Exist()
    {
        var state = CreateCartState();
        var carts = new Mock<ICartRepository>();
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.UserId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(state.Cart);
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.SessionId == "missing"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new MergeGuestCartCommandHandler(
            carts.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        var result = await handler.Handle(
            new MergeGuestCartCommand("missing"),
            CancellationToken.None);

        result.Id.Should().Be(state.Cart.Id);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada metrik kaydedicinin eksik günlük kayıtları oluşturup tüm sayaçları aynı adetle artırdığını doğruluyorum.
    [Fact]
    public async Task CartMetricsRecorder_Should_Create_Missing_Daily_Metrics()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product);
        var engagement = new Mock<IProductEngagementRepository>();
        ProductDailyMetric? productMetric = null;
        ProductVariantDailyMetric? variantMetric = null;
        engagement.Setup(repository => repository.GetProductDailyMetricForUpdateAsync(
                product.Id,
                new DateOnly(2026, 7, 23),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDailyMetric?)null);
        engagement.Setup(repository => repository.GetVariantDailyMetricForUpdateAsync(
                variant.Id,
                new DateOnly(2026, 7, 23),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductVariantDailyMetric?)null);
        engagement.Setup(repository => repository.AddProductDailyMetricAsync(
                It.IsAny<ProductDailyMetric>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProductDailyMetric, CancellationToken>((metric, _) => productMetric = metric)
            .Returns(Task.CompletedTask);
        engagement.Setup(repository => repository.AddVariantDailyMetricAsync(
                It.IsAny<ProductVariantDailyMetric>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProductVariantDailyMetric, CancellationToken>((metric, _) => variantMetric = metric)
            .Returns(Task.CompletedTask);
        var recorder = new CartMetricsRecorder(engagement.Object, new FixedClock());

        await recorder.RecordAddedQuantityAsync(
            product,
            variant,
            3,
            CancellationToken.None);

        product.TotalAddToCartCount.Should().Be(3);
        variant.AddToCartCount.Should().Be(3);
        productMetric.Should().NotBeNull();
        productMetric!.AddToCartCount.Should().Be(3);
        variantMetric.Should().NotBeNull();
        variantMetric!.AddToCartCount.Should().Be(3);
    }

    // Burada metrik kaydedicinin mevcut günlük kayıtları yeniden oluşturmadan güncellediğini doğruluyorum.
    [Fact]
    public async Task CartMetricsRecorder_Should_Reuse_Existing_Daily_Metrics()
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product);
        var date = new DateOnly(2026, 7, 23);
        var productMetric = new ProductDailyMetric(product.Id, date);
        var variantMetric = new ProductVariantDailyMetric(variant.Id, date);
        var engagement = new Mock<IProductEngagementRepository>();
        engagement.Setup(repository => repository.GetProductDailyMetricForUpdateAsync(
                product.Id,
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(productMetric);
        engagement.Setup(repository => repository.GetVariantDailyMetricForUpdateAsync(
                variant.Id,
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variantMetric);
        var recorder = new CartMetricsRecorder(engagement.Object, new FixedClock());

        await recorder.RecordAddedQuantityAsync(
            product,
            variant,
            2,
            CancellationToken.None);

        productMetric.AddToCartCount.Should().Be(2);
        variantMetric.AddToCartCount.Should().Be(2);
        engagement.Verify(repository => repository.AddProductDailyMetricAsync(
            It.IsAny<ProductDailyMetric>(),
            It.IsAny<CancellationToken>()), Times.Never);
        engagement.Verify(repository => repository.AddVariantDailyMetricAsync(
            It.IsAny<ProductVariantDailyMetric>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada testlerde kullanılacak aktif ürün örneğini dahili uzun kimliğiyle oluşturuyorum.
    private static Product CreateActiveProduct(long id = 12)
    {
        return new Product(
                "Cart Product",
                "cart-product",
                $"CART-{id}",
                status: ProductStatus.Active,
                isActive: true)
            .WithId(id);
    }

    // Burada test ürününe bağlı aktif ve stoklu varyant örneğini oluşturuyorum.
    private static ProductVariant CreateVariant(
        Product product,
        decimal price = 10m,
        int stock = 10)
    {
        return new ProductVariant(
            product.Id,
            "Default",
            $"SKU-{Guid.NewGuid():N}",
            price,
            stock);
    }

    // Burada sepet handler testleri için ilişkileri yüklenmiş tutarlı bir aggregate hazırlıyorum.
    private static CartState CreateCartState(
        int quantity = 2,
        decimal storedPrice = 10m,
        decimal currentPrice = 10m,
        int stock = 10)
    {
        var product = CreateActiveProduct();
        var variant = CreateVariant(product, currentPrice, stock);
        var cart = Cart.CreateForUser(7);
        var item = cart.AddItem(
            product.Id,
            variant.Id,
            quantity,
            storedPrice);
        AttachCatalog(item, product, variant);
        return new CartState(cart, item, product, variant);
    }

    // Burada EF tarafından yüklenecek Product ve ProductVariant navigationlarını unit test aggregate'ına bağlıyorum.
    private static void AttachCatalog(
        CartItem item,
        Product product,
        ProductVariant variant)
    {
        typeof(CartItem)
            .GetProperty(nameof(CartItem.Product))!
            .SetValue(item, product);
        typeof(CartItem)
            .GetProperty(nameof(CartItem.ProductVariant))!
            .SetValue(item, variant);
    }

    // Burada takipli sepet sorgusunu ve kayıt sonrası fallback okumasını sağlayan repository mockunu hazırlıyorum.
    private static Mock<ICartRepository> CreateTrackedCartRepository(Cart cart)
    {
        var repository = new Mock<ICartRepository>();
        repository.Setup(item => item.GetByOwnerForUpdateAsync(
                It.IsAny<CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        repository.Setup(item => item.GetByIdAsync(
                cart.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        return repository;
    }

    // Burada Add handlerının katalog, sepet, metrik ve transaction bağımlılıklarını birlikte hazırlıyorum.
    private static AddHandlerDependencies CreateAddHandlerDependencies(
        Product product,
        ProductVariant variant,
        Cart? cart)
    {
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var metrics = new Mock<ICartMetricsRecorder>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        carts.Setup(repository => repository.AddAsync(
                It.IsAny<Cart>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        carts.Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        products.Setup(repository => repository.GetByIdForUpdateAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        variants.Setup(repository => repository.GetByIdForUpdateAsync(
                variant.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        var currentUser = cart?.UserId is > 0
            ? new StubCurrentUser(cart.UserId)
            : new StubCurrentUser(null);
        var handler = new AddCartItemCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            new CartOwnerResolver(currentUser),
            metrics.Object,
            unitOfWork.Object);
        return new AddHandlerDependencies(
            handler,
            carts,
            metrics,
            unitOfWork);
    }

    // Burada serializable transaction delegesini gerçekten çalıştıran ve kayıt sonucunu taklit eden unit of work mocku oluşturuyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CartDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CartDto>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }

    // Burada merge testlerinde kullanıcı ve misafir owner sorgularını doğru aggregate'lara yönlendiriyorum.
    private static Mock<ICartRepository> CreateMergeRepository(
        Cart? userCart,
        Cart guestCart)
    {
        var repository = new Mock<ICartRepository>();
        if (userCart is not null)
        {
            repository.Setup(item => item.GetByOwnerForUpdateAsync(
                    It.Is<CartOwner>(owner => owner.UserId == userCart.UserId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(userCart);
            repository.Setup(item => item.GetByIdAsync(
                    userCart.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cart?)null);
        }

        repository.Setup(item => item.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.SessionId == guestCart.SessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestCart);
        return repository;
    }

    // Burada testlerde birlikte kullanılan sepet ve katalog aggregate'larını tek modelde taşıyorum.
    private sealed record CartState(
        Cart Cart,
        CartItem Item,
        Product Product,
        ProductVariant Variant);

    // Burada ekleme handlerı testlerinin mock bağımlılıklarını tek modelde taşıyorum.
    private sealed record AddHandlerDependencies(
        AddCartItemCommandHandler Handler,
        Mock<ICartRepository> Carts,
        Mock<ICartMetricsRecorder> Metrics,
        Mock<IUnitOfWork> UnitOfWork);

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada owner testleri için isteğe bağlı sabit kullanıcı kimliğini hazırlıyorum.
        public StubCurrentUser(long? userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
    }
}
