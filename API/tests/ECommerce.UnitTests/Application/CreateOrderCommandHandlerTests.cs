using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Commands.CreateOrder;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;
using System.Reflection;

namespace ECommerce.UnitTests.Application;

public sealed class CreateOrderCommandHandlerTests
{
    // Burada geçerli kullanıcı sepetinin stok düşümü, sipariş, metrik ve envanter hareketiyle atomik olarak işlendiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Create_Order_Reduce_Stock_And_Clear_Cart()
    {
        var product = CreateProduct(hasVariants: true);
        product.Images.Add(new ProductImage(
            product,
            "https://cdn.example.com/order-product.jpg",
            displayOrder: 1,
            isMain: true,
            altText: "Order product image"));
        var variant = CreateVariant(product, stock: 5, price: 12m, name: "Renk", value: "Pudra");
        var cart = Cart.CreateForUser(7);
        cart.AddItem(product.Id, variant.Id, 2, 10m);
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var orders = new Mock<IOrderRepository>();
        var addresses = new Mock<IAddressRepository>();
        var coupons = new Mock<ICouponRepository>();
        var metrics = new Mock<IOrderMetricsRecorder>();
        Order? savedOrder = null;
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<ECommerce.Application.Common.Models.CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        products.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        metrics.Setup(recorder => recorder.RecordPurchasedQuantitiesAsync(
                It.IsAny<IReadOnlyCollection<PurchaseMetricLine>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new CreateOrderCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            orders.Object,
            addresses.Object,
            metrics.Object,
            new OrderCouponService(coupons.Object, new FixedClock()),
            new StubCurrentUser(7),
            new FixedClock(),
            unitOfWork.Object);

        var result = await handler.Handle(new CreateOrderCommand(cart.ConcurrencyToken), CancellationToken.None);

        savedOrder.Should().NotBeNull();
        result.OrderNumber.Should().StartWith("ORD-");
        result.SubTotal.Should().Be(24m);
        savedOrder!.Items.Should().ContainSingle();
        result.Items.Single().ProductUrl.Should().Be("order-product");
        result.Items.Single().ImageUrl.Should().Be("https://cdn.example.com/order-product.jpg");
        result.Items.Single().ImageAlt.Should().Be("Order product image");
        result.Items.Single().VariantName.Should().Be("Renk");
        result.Items.Single().VariantValue.Should().Be("Pudra");
        savedOrder.Items.Single().VariantNameSnapshot.Should().Be("Renk");
        savedOrder.Items.Single().VariantValueSnapshot.Should().Be("Pudra");
        variant.Stock.Should().Be(3);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Sale &&
            movement.Direction == StockMovementDirection.Out &&
            movement.QuantityDelta == -2 &&
            movement.StockBeforeMovement == 5 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == savedOrder.Id);
        cart.Items.Should().BeEmpty();
        metrics.Verify(recorder => recorder.RecordPurchasedQuantitiesAsync(
            It.Is<IReadOnlyCollection<PurchaseMetricLine>>(lines =>
                lines.Count == 1 && lines.Single().Product == product && lines.Single().Variant == variant && lines.Single().Quantity == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(repository => repository.GetByIdsForUpdateAsync(
            It.IsAny<IEnumerable<long>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        variants.Verify(repository => repository.GetByIdsForUpdateAsync(
            It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(repository => repository.GetByIdForUpdateAsync(
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
        variants.Verify(repository => repository.GetByIdForUpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada eski sepet token'ıyla checkout isteğinin stok ve sipariş değiştirmeden reddedildiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Reject_Stale_Cart_Concurrency_Token()
    {
        var product = CreateProduct();
        var variant = CreateVariant(product);
        var cart = Cart.CreateForUser(7);
        cart.AddItem(product.Id, variant.Id, 1, variant.Price);
        var carts = new Mock<ICartRepository>();
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<ECommerce.Application.Common.Models.CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new CreateOrderCommandHandler(
            carts.Object,
            Mock.Of<IProductRepository>(),
            Mock.Of<IProductVariantRepository>(),
            Mock.Of<IOrderRepository>(),
            Mock.Of<IAddressRepository>(),
            Mock.Of<IOrderMetricsRecorder>(),
            new OrderCouponService(Mock.Of<ICouponRepository>(), new FixedClock()),
            new StubCurrentUser(7),
            new FixedClock(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new CreateOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        variant.Stock.Should().Be(10);
        cart.Items.Should().ContainSingle();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada kullanıcıya ait shipping adresi ve geçerli kuponun sipariş snapshot'ı ile güvenilir toplamda işlendiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Apply_Coupon_And_Snapshot_Owned_Shipping_Address()
    {
        var product = CreateProduct();
        var variant = CreateVariant(product, price: 100m);
        var cart = Cart.CreateForUser(7);
        cart.AddItem(product.Id, variant.Id, 1, 1m);
        var address = new Address(
            7,
            AddressType.Shipping,
            "Home",
            "Ada",
            "Yilmaz",
            "05000000000",
            "Izmir",
            "Konak",
            "Street 1");
        var coupon = new Coupon("SAVE20", CouponDiscountType.Percentage, 20m);
        var shippingMethod = new ShippingMethod("Standard", 0m);
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var orders = new Mock<IOrderRepository>();
        var addresses = new Mock<IAddressRepository>();
        var shippingMethods = new Mock<IShippingMethodRepository>();
        var coupons = new Mock<ICouponRepository>();
        var metrics = new Mock<IOrderMetricsRecorder>();
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<ECommerce.Application.Common.Models.CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        products.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        addresses.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                address.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        shippingMethods.Setup(repository => repository.GetByIdForUpdateAsync(
                shippingMethod.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingMethod);
        coupons.Setup(repository => repository.GetByCodeForUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        coupons.Setup(repository => repository.GetUsageForOrderForUpdateAsync(
                coupon.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CouponUsage?)null);
        coupons.Setup(repository => repository.AddUsageAsync(
                It.IsAny<CouponUsage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metrics.Setup(recorder => recorder.RecordPurchasedQuantitiesAsync(
                It.IsAny<IReadOnlyCollection<PurchaseMetricLine>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateOrderCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            orders.Object,
            addresses.Object,
            metrics.Object,
            new OrderCouponService(coupons.Object, new FixedClock()),
            new StubCurrentUser(7),
            new FixedClock(),
            CreateTransactionalUnitOfWork().Object,
            shippingMethodRepository: shippingMethods.Object);

        var result = await handler.Handle(
            new CreateOrderCommand(cart.ConcurrencyToken, address.Id, " save20 ", shippingMethod.Id),
            CancellationToken.None);

        result.SubTotal.Should().Be(100m);
        result.DiscountTotal.Should().Be(20m);
        result.GrandTotal.Should().Be(80m);
        result.CouponCode.Should().Be("SAVE20");
        result.ShippingAddress.Should().NotBeNull();
        result.ShippingAddress!.SourceAddressId.Should().Be(address.Id);
        result.Items.Single().VariantName.Should().BeNull();
        result.Items.Single().VariantValue.Should().BeNull();
        coupon.UsedCount.Should().Be(1);
        coupons.Verify(repository => repository.AddUsageAsync(
            It.Is<CouponUsage>(usage => usage.OrderId.HasValue && usage.UserId == 7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada checkout'un ürün vergi oranı ve seçilmiş etkin kargo ücretini güvenilir snapshot toplamına eklediğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Calculate_Tax_And_Use_The_Selected_Active_Shipping_Method()
    {
        var taxRate = new TaxRate("KDV", 20m);
        var product = CreateProduct(taxRate);
        var variant = CreateVariant(product, price: 120m);
        variant.RecalculateNetPrice(taxRate);
        var cart = Cart.CreateForUser(7);
        cart.AddItem(product.Id, variant.Id, 1, 1m);
        var address = new Address(
            7,
            AddressType.Shipping,
            "Home",
            "Ada",
            "Yilmaz",
            "05000000000",
            "Izmir",
            "Konak",
            "Street 1");
        var shippingMethod = new ShippingMethod("Express", 15m);
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var orders = new Mock<IOrderRepository>();
        var addresses = new Mock<IAddressRepository>();
        var shippingMethods = new Mock<IShippingMethodRepository>();
        var metrics = new Mock<IOrderMetricsRecorder>();
        var notifications = new Mock<IOrderNotificationService>();
        Order? savedOrder = null;
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<ECommerce.Application.Common.Models.CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        products.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        addresses.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                address.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        shippingMethods.Setup(repository => repository.GetByIdForUpdateAsync(
                shippingMethod.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingMethod);
        metrics.Setup(recorder => recorder.RecordPurchasedQuantitiesAsync(
                It.IsAny<IReadOnlyCollection<PurchaseMetricLine>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        notifications.Setup(service => service.QueueOrderCreatedAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateOrderCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            orders.Object,
            addresses.Object,
            metrics.Object,
            new OrderCouponService(Mock.Of<ICouponRepository>(), new FixedClock()),
            new StubCurrentUser(7),
            new FixedClock(),
            CreateTransactionalUnitOfWork().Object,
            shippingMethodRepository: shippingMethods.Object,
            pricingService: new OrderPricingService(),
            notificationService: notifications.Object);

        var result = await handler.Handle(
            new CreateOrderCommand(cart.ConcurrencyToken, address.Id, null, shippingMethod.Id),
            CancellationToken.None);

        result.SubTotal.Should().Be(100m);
        result.DiscountTotal.Should().Be(0m);
        result.ShippingTotal.Should().Be(15m);
        result.TaxTotal.Should().Be(20m);
        result.GrandTotal.Should().Be(135m);
        result.ShippingMethodName.Should().Be("Express");
        result.Items.Single().TaxRatePercentage.Should().Be(20m);
        result.Items.Single().TaxTotal.Should().Be(20m);
        savedOrder!.ReservationExpiresAt.Should().NotBeNull();
        notifications.Verify(service => service.QueueOrderCreatedAsync(
            savedOrder,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada yüzde yüz indirimli siparişin ödeme kaydı oluşturmadan güvenli biçimde paid durumuna geçtiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_AutoComplete_A_Fully_Discounted_Order()
    {
        var product = CreateProduct();
        var variant = CreateVariant(product, price: 100m);
        var cart = Cart.CreateForUser(7);
        cart.AddItem(product.Id, variant.Id, 1, variant.Price);
        var coupon = new Coupon("FREE100", CouponDiscountType.Percentage, 100m);
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var orders = new Mock<IOrderRepository>();
        var coupons = new Mock<ICouponRepository>();
        var metrics = new Mock<IOrderMetricsRecorder>();
        Order? savedOrder = null;
        carts.Setup(repository => repository.GetByOwnerForUpdateAsync(
                It.IsAny<ECommerce.Application.Common.Models.CartOwner>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        products.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        coupons.Setup(repository => repository.GetByCodeForUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        coupons.Setup(repository => repository.GetUsageForOrderForUpdateAsync(
                coupon.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CouponUsage?)null);
        coupons.Setup(repository => repository.AddUsageAsync(
                It.IsAny<CouponUsage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metrics.Setup(recorder => recorder.RecordPurchasedQuantitiesAsync(
                It.IsAny<IReadOnlyCollection<PurchaseMetricLine>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        var handler = new CreateOrderCommandHandler(
            carts.Object,
            products.Object,
            variants.Object,
            orders.Object,
            Mock.Of<IAddressRepository>(),
            metrics.Object,
            new OrderCouponService(coupons.Object, new FixedClock()),
            new StubCurrentUser(7),
            new FixedClock(),
            CreateTransactionalUnitOfWork().Object);

        var result = await handler.Handle(
            new CreateOrderCommand(cart.ConcurrencyToken, null, "free100"),
            CancellationToken.None);

        result.GrandTotal.Should().Be(0m);
        result.Status.Should().Be(OrderStatus.Paid);
        result.PaidAt.Should().Be(new FixedClock().UtcNow);
        savedOrder.Should().NotBeNull();
        savedOrder!.Payments.Should().BeEmpty();
        coupon.UsedCount.Should().Be(1);
    }

    // Burada test için satışa açık ürün örneğini kalıcı kimliğiyle oluşturuyorum.
    private static Product CreateProduct(TaxRate? taxRate = null, bool hasVariants = false)
    {
        var product = new Product(
            "Order Product",
            "order-product",
            "ORDER-MAIN",
            status: ProductStatus.Active,
            taxRateId: taxRate?.Id,
            hasVariants: hasVariants)
            .WithId(12);
        if (taxRate is not null)
        {
            typeof(Product)
                .GetProperty(nameof(Product.TaxRate), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(product, taxRate);
        }

        return product;
    }

    // Burada test için stoklu aktif varyant örneğini oluşturuyorum.
    private static ProductVariant CreateVariant(
        Product product,
        int stock = 10,
        decimal price = 10m,
        string name = "Default",
        string? value = null)
    {
        return new ProductVariant(
            product.Id,
            name,
            $"ORDER-{Guid.NewGuid():N}",
            price,
            stock,
            value: value);
    }

    // Burada serializable transaction delegesini testte doğrudan çalıştıran unit of work mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<OrderDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OrderDto>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada test isteğinin sabit kullanıcı kimliğini hazırlıyorum.
        public StubCurrentUser(long userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
    }
}
