using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.ExpireStockReservations;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Payments;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ReservationAndReturnWorkflowTests
{
    // Burada süresi dolmuş ödeme öncesi rezervasyonun siparişi iptal edip stok ve kupon serbest bırakma akışını çalıştırdığını doğruluyorum.
    [Fact]
    public async Task ExpireStockReservations_Should_Restore_Stock_And_Cancel_The_Order()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateReservedOrder(clock.UtcNow.AddMinutes(-16), stock: 2);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetExpiredStockReservationsAsync(
                clock.UtcNow,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var unitOfWork = new ImmediateUnitOfWork();
        var inventory = new OrderInventoryService(variants.Object);
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), clock);
        var handler = new ExpireStockReservationsCommandHandler(
            orders.Object,
            inventory,
            coupons,
            clock,
            unitOfWork,
            CreateDefinitivePaymentFailureService(inventory, coupons, clock),
            Mock.Of<IAuthoritativeSalesMetricService>());

        var result = await handler.Handle(new ExpireStockReservationsCommand(), CancellationToken.None);

        result.CancelledOrderCount.Should().Be(1);
        result.SkippedPendingPaymentCount.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Cancelled);
        variant.Stock.Should().Be(3);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 1 &&
            movement.StockBeforeMovement == 2 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == order.Id);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    // Burada belirsiz sağlayıcı sonucu taşıyan pending ödeme için rezervasyon işçisinin stok değiştirmediğini doğruluyorum.
    [Fact]
    public async Task ExpireStockReservations_Should_Skip_A_Pending_Payment()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateReservedOrder(clock.UtcNow.AddMinutes(-16), stock: 2);
        order.AddPayment(new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "reservation_worker_pending_01"));
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetExpiredStockReservationsAsync(
                clock.UtcNow,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        var variants = new Mock<IProductVariantRepository>();
        var unitOfWork = new ImmediateUnitOfWork();
        var inventory = new OrderInventoryService(variants.Object);
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), clock);
        var handler = new ExpireStockReservationsCommandHandler(
            orders.Object,
            inventory,
            coupons,
            clock,
            unitOfWork,
            CreateDefinitivePaymentFailureService(inventory, coupons, clock),
            Mock.Of<IAuthoritativeSalesMetricService>());

        var result = await handler.Handle(new ExpireStockReservationsCommand(), CancellationToken.None);

        result.CancelledOrderCount.Should().Be(0);
        result.SkippedPendingPaymentCount.Should().Be(1);
        order.Status.Should().Be(OrderStatus.Pending);
        variant.Stock.Should().Be(2);
        variants.Verify(repository => repository.GetByIdsForUpdateAsync(
            It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    // Burada sağlayıcı iptali kesinleştirdiğinde bekleyen ödemenin başarısız kapanıp stok rezervasyonunun serbest kaldığını doğruluyorum.
    [Fact]
    public async Task ExpireStockReservations_Should_Cancel_A_ProviderConfirmed_Pending_Payment_And_Release_Stock()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateReservedOrder(clock.UtcNow.AddMinutes(-16), stock: 2);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "reservation_reconciled_cancel_01");
        order.AddPayment(payment);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetExpiredStockReservationsAsync(
                clock.UtcNow,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var reconciler = new CancelledPaymentReconciler(PaymentProvider.Fake);
        var unitOfWork = new ImmediateUnitOfWork();
        var inventory = new OrderInventoryService(variants.Object);
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), clock);
        var handler = new ExpireStockReservationsCommandHandler(
            orders.Object,
            inventory,
            coupons,
            clock,
            unitOfWork,
            CreateDefinitivePaymentFailureService(inventory, coupons, clock),
            Mock.Of<IAuthoritativeSalesMetricService>(),
            [reconciler]);

        var result = await handler.Handle(new ExpireStockReservationsCommand(), CancellationToken.None);

        result.CancelledOrderCount.Should().Be(1);
        result.SkippedPendingPaymentCount.Should().Be(0);
        payment.Status.Should().Be(PaymentStatus.Failed);
        order.Status.Should().Be(OrderStatus.Cancelled);
        variant.Stock.Should().Be(3);
        reconciler.CallCount.Should().Be(1);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    // Burada callback iptali yarışı kazandığında rezervasyon worker'ının ikinci stok veya sipariş mutasyonu üretmediğini doğruluyorum.
    [Fact]
    public async Task ExpireStockReservations_Should_Be_Idempotent_When_Callback_Already_Cancelled_Order()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateReservedOrder(clock.UtcNow.AddMinutes(-16), stock: 2);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "reservation_callback_race_01");
        order.AddPayment(payment);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetExpiredStockReservationsAsync(
                clock.UtcNow,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var inventory = new OrderInventoryService(variants.Object);
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), clock);
        var failureService = CreateDefinitivePaymentFailureService(inventory, coupons, clock);
        var callbackApplied = await failureService.ApplyAsync(
            order,
            payment,
            "Signed callback failure.",
            "provider-failure-race-01",
            CancellationToken.None);
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new ExpireStockReservationsCommandHandler(
            orders.Object,
            inventory,
            coupons,
            clock,
            unitOfWork,
            failureService,
            Mock.Of<IAuthoritativeSalesMetricService>(),
            [new CancelledPaymentReconciler(PaymentProvider.Fake)]);

        var result = await handler.Handle(new ExpireStockReservationsCommand(), CancellationToken.None);

        callbackApplied.Should().BeTrue();
        result.CancelledOrderCount.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Cancelled);
        payment.Status.Should().Be(PaymentStatus.Failed);
        variant.Stock.Should().Be(3);
        variant.StockMovements.Should().ContainSingle(movement => movement.Type == StockMovementType.Cancellation);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    // Burada sağlayıcı ödemeyi başarılı doğrularsa rezervasyonun iptal edilmediğini ve siparişin paid durumuna geçtiğini doğruluyorum.
    [Fact]
    public async Task ExpireStockReservations_Should_Keep_Stock_Reserved_When_Provider_Reconciles_As_Paid()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateReservedOrder(clock.UtcNow.AddMinutes(-16), stock: 2);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "reservation_reconciled_paid_01");
        order.AddPayment(payment);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetExpiredStockReservationsAsync(
                clock.UtcNow,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var reconciler = new PaidPaymentReconciler(PaymentProvider.Fake);
        var unitOfWork = new ImmediateUnitOfWork();
        var inventory = new OrderInventoryService(Mock.Of<IProductVariantRepository>());
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), clock);
        var handler = new ExpireStockReservationsCommandHandler(
            orders.Object,
            inventory,
            coupons,
            clock,
            unitOfWork,
            CreateDefinitivePaymentFailureService(inventory, coupons, clock),
            Mock.Of<IAuthoritativeSalesMetricService>(),
            [reconciler]);

        var result = await handler.Handle(new ExpireStockReservationsCommand(), CancellationToken.None);

        result.CancelledOrderCount.Should().Be(0);
        result.ReconciledPaidOrderCount.Should().Be(1);
        payment.Status.Should().Be(PaymentStatus.Paid);
        order.Status.Should().Be(OrderStatus.Paid);
        order.ReservationExpiresAt.Should().BeNull();
        variant.Stock.Should().Be(2);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    // Burada müşterinin yalnız teslim edilmiş siparişte aynı ürüne ait stoklu replacement ile değişim talebi açabildiğini doğruluyorum.
    [Fact]
    public async Task CreateReturnRequest_Should_Create_A_Valid_Exchange_Request_For_A_Delivered_Order()
    {
        var clock = new FixedClock();
        var (order, originalVariant) = CreateDeliveredOrder(clock);
        var replacementVariant = new ProductVariant(
            originalVariant.ProductId,
            "Replacement",
            "RETURN-REPLACEMENT-SKU",
            originalVariant.Price,
            4);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        returnRequests.Setup(repository => repository.AddAsync(
                It.IsAny<ReturnRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([replacementVariant]);
        var handler = new CreateReturnRequestCommandHandler(
            orders.Object,
            returnRequests.Object,
            variants.Object,
            new StubCurrentUser(7),
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);
        var orderItem = order.Items.Single();

        var result = await handler.Handle(
            new CreateReturnRequestCommand(
                order.Id,
                ReturnType.Exchange,
                [new CreateReturnItemCommand(orderItem.Id, 1, replacementVariant.Id)]),
            CancellationToken.None);

        result.Type.Should().Be(ReturnType.Exchange);
        result.Status.Should().Be(ReturnRequestStatus.Requested);
        result.RefundTotal.Should().Be(0m);
        result.Items.Should().ContainSingle(item => item.ReplacementProductVariantId == replacementVariant.Id);
        returnRequests.Verify(repository => repository.AddAsync(
            It.IsAny<ReturnRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada önceki kısmi iade adetleri varken indirim ve vergi dahil tutarın yuvarlama farkını aşmadan sıradaki adetlere paylaştırdığını doğruluyorum.
    [Fact]
    public async Task CreateReturnRequest_Should_Allocate_Tax_And_Discount_Aware_Refunds_Without_Exceeding_The_Order_Item_Total()
    {
        var clock = new FixedClock();
        var (order, orderItem) = CreateDeliveredOrderWithTaxAndDiscount(clock);
        var previousRequest = new ReturnRequest(order.Id, 7, "RET-PREVIOUS-PARTIAL", ReturnType.Refund);
        previousRequest.AddItem(orderItem, 1);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([previousRequest]);
        returnRequests.Setup(repository => repository.AddAsync(
                It.IsAny<ReturnRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateReturnRequestCommandHandler(
            orders.Object,
            returnRequests.Object,
            Mock.Of<IProductVariantRepository>(),
            new StubCurrentUser(7),
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        var result = await handler.Handle(
            new CreateReturnRequestCommand(
                order.Id,
                ReturnType.Refund,
                [new CreateReturnItemCommand(orderItem.Id, 1)]),
            CancellationToken.None);

        previousRequest.RefundTotal.Should().Be(35.40m);
        result.RefundTotal.Should().Be(35.39m);
        (previousRequest.RefundTotal + result.RefundTotal).Should().BeLessThanOrEqualTo(orderItem.RefundTotal);
    }

    // Burada teslim sonrası onaylanan para iadesinin satış iadesi türünde pozitif stok hareketi yazdığını doğruluyorum.
    [Fact]
    public async Task RestockApprovedRefund_Should_Record_An_Incoming_Sale_Return_Movement()
    {
        var clock = new FixedClock();
        var (order, variant) = CreateDeliveredOrder(clock);
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-REFUND-RESTOCK", ReturnType.Refund);
        returnRequest.AddItem(order.Items.Single(), 1);
        returnRequest.Receive(clock.UtcNow);
        returnRequest.Approve(clock.UtcNow.AddMinutes(1));
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var service = new ReturnInventoryService(variants.Object);

        await service.RestockRefundAsync(returnRequest, CancellationToken.None);

        variant.Stock.Should().Be(3);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.SaleReturn &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 1 &&
            movement.StockBeforeMovement == 2 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == order.Id &&
            movement.ReturnRequestId == returnRequest.Id);
    }

    // Burada değişimin tamamlanmasında iade stok girişinin ve replacement stok çıkışının birer kez uygulandığını doğruluyorum.
    [Fact]
    public async Task FulfillExchange_Should_Restore_Original_And_Reduce_Replacement_Stock_Once()
    {
        var clock = new FixedClock();
        var (order, originalVariant) = CreateDeliveredOrder(clock);
        var replacementVariant = new ProductVariant(
            originalVariant.ProductId,
            "Replacement",
            "RETURN-REPLACEMENT-FULFILL-SKU",
            originalVariant.Price,
            4);
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-EXCHANGE-FULFILL", ReturnType.Exchange);
        returnRequest.AddItem(order.Items.Single(), 1, replacementVariant.Id);
        returnRequest.Receive(clock.UtcNow);
        returnRequest.Approve(clock.UtcNow.AddMinutes(1));
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([originalVariant, replacementVariant]);
        var service = new ReturnInventoryService(variants.Object);

        await service.FulfillExchangeAsync(returnRequest, CancellationToken.None);

        originalVariant.Stock.Should().Be(3);
        replacementVariant.Stock.Should().Be(3);
        originalVariant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.SaleReturn &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 1 &&
            movement.StockBeforeMovement == 2 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == order.Id &&
            movement.ReturnRequestId == returnRequest.Id);
        replacementVariant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Sale &&
            movement.Direction == StockMovementDirection.Out &&
            movement.QuantityDelta == -1 &&
            movement.StockBeforeMovement == 4 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == order.Id &&
            movement.ReturnRequestId == returnRequest.Id);
    }

    // Burada aynı replacement varyanta yönelen değişim kalemlerinin tek toplam stok çıkışı ürettiğini doğruluyorum.
    [Fact]
    public async Task FulfillExchange_Should_Group_Replacement_Quantities_By_Variant()
    {
        var clock = new FixedClock();
        var product = new Product(
            "Grouped Exchange Product",
            "grouped-exchange-product",
            "GROUPED-EXCHANGE-MAIN",
            status: ProductStatus.Active)
            .WithId(12);
        var firstOriginal = new ProductVariant(
            product.Id,
            "First Original",
            "GROUPED-ORIGINAL-ONE",
            10m,
            2);
        var secondOriginal = new ProductVariant(
            product.Id,
            "Second Original",
            "GROUPED-ORIGINAL-TWO",
            10m,
            2);
        var replacement = new ProductVariant(
            product.Id,
            "Shared Replacement",
            "GROUPED-REPLACEMENT",
            10m,
            5);
        var order = new Order(
            7,
            $"ORD-{Guid.NewGuid():N}"[..24],
            20m,
            0m,
            0m,
            0m,
            20m);
        order.AddItem(product.Id, firstOriginal.Id, product.Title, firstOriginal.Sku, 10m, 1);
        order.AddItem(product.Id, secondOriginal.Id, product.Title, secondOriginal.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        var returnRequest = new ReturnRequest(
            order.Id,
            7,
            "RET-GROUPED-EXCHANGE",
            ReturnType.Exchange);
        foreach (var orderItem in order.Items)
        {
            returnRequest.AddItem(orderItem, 1, replacement.Id);
        }

        returnRequest.Receive(clock.UtcNow);
        returnRequest.Approve(clock.UtcNow.AddMinutes(1));
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstOriginal, secondOriginal, replacement]);
        var service = new ReturnInventoryService(variants.Object);

        await service.FulfillExchangeAsync(returnRequest, CancellationToken.None);

        firstOriginal.Stock.Should().Be(3);
        secondOriginal.Stock.Should().Be(3);
        replacement.Stock.Should().Be(3);
        replacement.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Sale &&
            movement.Direction == StockMovementDirection.Out &&
            movement.QuantityDelta == -2 &&
            movement.StockBeforeMovement == 5 &&
            movement.StockAfterMovement == 3 &&
            movement.OrderId == order.Id &&
            movement.ReturnRequestId == returnRequest.Id);
    }

    // Burada ödeme bekleyen stok rezervasyonu için teslim edilmiş sipariş state'i olmayan temel aggregate'ı hazırlıyorum.
    private static (Order Order, ProductVariant Variant) CreateReservedOrder(DateTime reservedAt, int stock)
    {
        var product = new Product("Reservation Product", "reservation-product", "RESERVATION-MAIN", status: ProductStatus.Active)
            .WithId(12);
        var variant = new ProductVariant(product.Id, "Default", "RESERVATION-SKU", 10m, stock);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 10m, 0m, 0m, 0m, 10m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        order.StartStockReservation(reservedAt, TimeSpan.FromMinutes(15));
        return (order, variant);
    }

    // Burada testin müşteri iadesi kurallarını çalıştıracağı ödenmiş ve teslim edilmiş siparişi hazırlıyorum.
    private static (Order Order, ProductVariant Variant) CreateDeliveredOrder(FixedClock clock)
    {
        var product = new Product("Return Product", "return-product", "RETURN-MAIN", status: ProductStatus.Active)
            .WithId(12);
        var variant = new ProductVariant(product.Id, "Original", "RETURN-ORIGINAL-SKU", 10m, 2);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 10m, 0m, 0m, 0m, 10m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, clock.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "return_delivered_payment_01");
        order.AddPayment(payment);
        payment.MarkAsPaid("return_delivered_payment_transaction_01");
        order.ChangeStatus(OrderStatus.Paid, clock.UtcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, clock.UtcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, clock.UtcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, clock.UtcNow.AddMinutes(4));
        return (order, variant);
    }

    // Burada kısmi iade yuvarlama testinde kullanılacak indirim ve vergi snapshot'lı teslim edilmiş siparişi hazırlıyorum.
    private static (Order Order, OrderItem Item) CreateDeliveredOrderWithTaxAndDiscount(FixedClock clock)
    {
        var product = new Product("Refund Product", "refund-product", "REFUND-MAIN", status: ProductStatus.Active)
            .WithId(13);
        var variant = new ProductVariant(product.Id, "Original", "REFUND-ORIGINAL-SKU", 33.33m, 3);
        var order = new Order(7, "ORD-REFUND-PARTIAL", 99.99m, 10m, 0m, 16.20m, 106.19m);
        var item = order.AddItem(
            product.Id,
            variant.Id,
            product.Title,
            variant.Sku,
            33.33m,
            3,
            discountTotal: 10m,
            taxRatePercentage: 18m,
            taxTotal: 16.20m);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, clock.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "partial_refund_payment_01");
        order.AddPayment(payment);
        payment.MarkAsPaid("partial_refund_transaction_01");
        order.ChangeStatus(OrderStatus.Paid, clock.UtcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, clock.UtcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, clock.UtcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, clock.UtcNow.AddMinutes(4));
        return (order, item);
    }

    // Burada serializable transaction delegesini testte doğrudan çalıştıracak generic unit of work mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork<TResponse>()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TResponse>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TResponse>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    // Burada rezervasyon worker testlerinin ortak kesin başarısızlık servisini zararsız bildirim mockuyla hazırlıyorum.
    private static DefinitivePaymentFailureService CreateDefinitivePaymentFailureService(
        OrderInventoryService inventory,
        OrderCouponService coupons,
        IDateTimeProvider clock)
    {
        var notifications = new Mock<IOrderNotificationService>();
        notifications.Setup(service => service.QueuePaymentResultAsync(
                It.IsAny<Order>(),
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications.Setup(service => service.QueueOrderStatusChangedAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new DefinitivePaymentFailureService(inventory, coupons, notifications.Object, clock);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        // Burada generic transaction çağrılarını testte doğrudan çalıştıracak hafif unit of work örneğini hazırlıyorum.
        public ImmediateUnitOfWork()
        {
        }

        public int SaveChangesCallCount { get; private set; }

        // Burada testin kaydetme çağrılarını sayıp başarılı sonuç döndürüyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        // Burada farklı generic sonuç türleriyle çalışan transaction delegesini testte doğrudan yürütüyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class CancelledPaymentReconciler : IPaymentGatewayReconciler
    {
        // Burada testte sağlayıcının bekleyen ödeme denemesini güvenle iptal ettiğini modellemek için türü hazırlıyorum.
        public CancelledPaymentReconciler(PaymentProvider provider)
        {
            Provider = provider;
        }

        public PaymentProvider Provider { get; }
        public int CallCount { get; private set; }

        // Burada testte provider-mutabakat çağrısını sayıp kesin iptal sonucu döndürüyorum.
        public Task<PaymentReconciliationResult> ReconcilePendingPaymentAsync(
            PaymentReconciliationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new PaymentReconciliationResult(PaymentReconciliationStatus.Cancelled));
        }
    }

    private sealed class PaidPaymentReconciler : IPaymentGatewayReconciler
    {
        // Burada testte sağlayıcının bekleyen ödeme denemesini başarılı olarak doğruladığı türü hazırlıyorum.
        public PaidPaymentReconciler(PaymentProvider provider)
        {
            Provider = provider;
        }

        public PaymentProvider Provider { get; }

        // Burada testte geçerli işlem kimliğiyle kesin başarılı provider sonucu döndürüyorum.
        public Task<PaymentReconciliationResult> ReconcilePendingPaymentAsync(
            PaymentReconciliationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentReconciliationResult(
                PaymentReconciliationStatus.Paid,
                "reconciled_payment_transaction_01"));
        }
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada test akışının oturum kullanıcı kimliğini sabitliyorum.
        public StubCurrentUser(long userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    }
}
