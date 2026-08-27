using ECommerce.Application.Addresses.Commands.SetDefaultAddress;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.CreatePayment;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class OrderPaymentWorkflowPersistenceTests
{
    // Burada aynı kullanıcı ve adres türü için veritabanının ikinci varsayılan adresi reddettiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Multiple_Default_Addresses_For_The_Same_User_And_Type()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "default-address@example.com");
        context.Addresses.AddRange(CreateAddress(user.Id, true), CreateAddress(user.Id, true));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada ödeme retry anahtarının aynı sipariş için ikinci ödeme kaydını veritabanı düzeyinde reddettiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_Payment_Idempotency_Key_Per_Order()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "payment-idempotency@example.com");
        var order = CreateOrder(user.Id);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        context.Payments.AddRange(
            new Payment(order.Id, PaymentProvider.Fake, 10m, "payment_idempotency_key_01"),
            new Payment(order.Id, PaymentProvider.Fake, 10m, "PAYMENT_IDEMPOTENCY_KEY_01"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada terk edilmiş CheckoutForm denetim alanlarının kalıcı olduğunu ve repository'nin yalnız zamanı gelen tokenı seçtiğini doğruluyorum.
    [Fact]
    public async Task Repository_Should_Persist_And_Query_Due_Abandoned_Checkout_Form()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var clock = new FixedClock();
        var user = await SeedUserAsync(context, "abandoned-checkout@example.com");
        var order = CreateOrder(user.Id);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "abandoned_persistence_01");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "abandoned-persistence-token",
            payment.Id.ToString("N"),
            "https://sandbox-cpp.iyzipay.com?token=abandoned-persistence-token",
            DateTime.UtcNow.AddMinutes(30));
        payment.AbandonCheckoutForm(clock.UtcNow);
        order.ChangeStatus(OrderStatus.Cancelled, clock.UtcNow);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new OrderRepository(context);

        var dueTokens = await repository.GetDueAbandonedPaymentTokensAsync(clock.UtcNow, 10);

        dueTokens.Should().ContainSingle().Which.Should().Be("abandoned-persistence-token");
        var savedOrder = await repository.GetByPaymentProviderTokenAsync(
            "abandoned-persistence-token",
            true);
        var savedPayment = savedOrder!.Payments.Single();
        savedPayment.CustomerAbandonedAt.Should().Be(clock.UtcNow);
        savedPayment.CompleteAbandonmentReconciliation(clock.UtcNow.AddMinutes(1));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        (await repository.GetDueAbandonedPaymentTokensAsync(clock.UtcNow.AddMinutes(2), 10)).Should().BeEmpty();
    }

    // Burada varsayılan adresin birinden diğerine geçişinin filtered unique indeks altında güvenle tamamlandığını doğruluyorum.
    [Fact]
    public async Task SetDefaultAddress_Should_Safely_Replace_The_Previous_Default()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "default-switch@example.com");
        var previousDefault = CreateAddress(user.Id, true);
        var selectedAddress = CreateAddress(user.Id, false);
        context.Addresses.AddRange(previousDefault, selectedAddress);
        await context.SaveChangesAsync();
        var handler = new SetDefaultAddressCommandHandler(
            new AddressRepository(context),
            new StubCurrentUser(user.Id),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SetDefaultAddressCommand(selectedAddress.Id),
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var addresses = await context.Addresses
            .AsNoTracking()
            .Where(address => address.UserId == user.Id && address.Type == AddressType.Shipping)
            .ToListAsync();
        result.IsDefault.Should().BeTrue();
        addresses.Should().ContainSingle(address => address.Id == selectedAddress.Id && address.IsDefault);
        addresses.Should().ContainSingle(address => address.Id == previousDefault.Id && !address.IsDefault);
    }

    // Burada sipariş repository'sinin başka kullanıcıya ait siparişi owner kapsamı dışında döndürmediğini doğruluyorum.
    [Fact]
    public async Task OrderRepository_Should_Not_Return_Another_Users_Order()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var orderOwner = await SeedUserAsync(context, "order-owner@example.com");
        var otherUser = await SeedUserAsync(context, "order-other@example.com");
        var order = CreateOrder(orderOwner.Id);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var repository = new OrderRepository(context);

        var unauthorizedResult = await repository.GetByIdForUserAsync(order.Id, otherUser.Id);
        var ownerResult = await repository.GetByIdForUserAsync(order.Id, orderOwner.Id);

        unauthorizedResult.Should().BeNull();
        ownerResult.Should().NotBeNull();
    }

    // Burada ödeme handler'ının mevcut siparişe denemeyi Added olarak kaydedip ikinci aşamada paid durumuna taşıdığını doğruluyorum.
    [Fact]
    public async Task CreatePaymentHandler_Should_Persist_And_Complete_The_Payment_Attempt()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "payment-handler@example.com");
        var order = CreateOrder(user.Id);
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var handler = new CreatePaymentCommandHandler(
            new OrderRepository(context),
            [new SuccessfulPaymentGateway()],
            new StubCurrentUser(user.Id),
            new FixedClock(),
            new UnitOfWork(context),
            new AuthoritativeSalesMetricService(new ProductRepository(context)));

        var result = await handler.Handle(
            new CreatePaymentCommand(order.Id, PaymentProvider.Fake, "payment_handler_key_0001"),
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var savedOrder = await context.Orders
            .AsNoTracking()
            .Include(savedOrder => savedOrder.Payments)
            .SingleAsync(savedOrder => savedOrder.Id == order.Id);
        result.Status.Should().Be(PaymentStatus.Paid);
        savedOrder.Status.Should().Be(OrderStatus.Paid);
        savedOrder.Payments.Should().ContainSingle(payment => payment.Status == PaymentStatus.Paid);
    }

    // Burada kesin ödeme başarısızlığının stok, kupon, sipariş, ödeme ve outbox kayıtlarını tek transaction'da kalıcılaştırdığını doğruluyorum.
    [Fact]
    public async Task DefinitivePaymentFailure_Should_Persist_Atomic_Cancellation_And_Be_Idempotent()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        var clock = new FixedClock();
        Guid orderId;
        Guid paymentId;
        Guid variantId;
        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = await SeedUserAsync(seedContext, "definitive-failure@example.com");
            var catalog = await SeedCatalogAsync(seedContext, "definitive-failure");
            var coupon = new Coupon("FAIL10", CouponDiscountType.FixedAmount, 1m);
            coupon.IncreaseUsedCount(clock.UtcNow);
            var order = new Order(
                user.Id,
                $"ORD-{Guid.NewGuid():N}"[..24],
                10m,
                1m,
                0m,
                0m,
                9m,
                couponCode: coupon.Code);
            order.SetCustomerSnapshot("Ada", "Lovelace", user.Email, "+905551112233");
            order.AddItem(
                catalog.Product.Id,
                catalog.Variant.Id,
                catalog.Product.Title,
                catalog.Variant.Sku,
                10m,
                1,
                discountTotal: 1m);
            order.EnsureItemsMatchSubTotal();
            order.StartStockReservation(clock.UtcNow, TimeSpan.FromMinutes(15));
            catalog.Variant.ApplyStockMovement(
                -1,
                StockMovementType.Sale,
                "Checkout reservation.",
                order.Id);
            var payment = new Payment(
                order.Id,
                PaymentProvider.Iyzico,
                order.GrandTotal,
                "definitive_failure_persistence_01");
            order.AddPayment(payment);
            payment.InitializeCheckoutForm(
                "definitive-failure-token",
                payment.Id.ToString("N"),
                "https://sandbox-api.iyzipay.com/checkoutform/definitive-failure-token",
                DateTime.UtcNow.AddMinutes(30));
            seedContext.AddRange(coupon, order, new CouponUsage(coupon.Id, user.Id, order.Id, clock.UtcNow));
            await seedContext.SaveChangesAsync();
            orderId = order.Id;
            paymentId = payment.Id;
            variantId = catalog.Variant.Id;
        }

        await using (var mutationContext = new AppDbContext(options))
        {
            var orders = new OrderRepository(mutationContext);
            var inventory = new OrderInventoryService(new ProductVariantRepository(mutationContext));
            var coupons = new OrderCouponService(new CouponRepository(mutationContext), clock);
            var notifications = new OrderNotificationService(
                new UserRepository(mutationContext),
                new EmailOutboxRepository(mutationContext),
                clock);
            var failureService = new DefinitivePaymentFailureService(inventory, coupons, notifications, clock);
            var unitOfWork = new UnitOfWork(mutationContext);

            var firstApplied = await unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
            {
                var order = await orders.GetByIdForUpdateAsync(orderId, token);
                var payment = order!.Payments.Single(candidate => candidate.Id == paymentId);
                var applied = await failureService.ApplyAsync(
                    order,
                    payment,
                    "Signed provider failure.",
                    "provider-failure-001",
                    token);
                await unitOfWork.SaveChangesAsync(token);
                return applied;
            });
            mutationContext.ChangeTracker.Clear();
            var replayApplied = await unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
            {
                var order = await orders.GetByIdForUpdateAsync(orderId, token);
                var payment = order!.Payments.Single(candidate => candidate.Id == paymentId);
                return await failureService.ApplyAsync(
                    order,
                    payment,
                    "Signed provider failure.",
                    "provider-failure-001",
                    token);
            });

            firstApplied.Should().BeTrue();
            replayApplied.Should().BeFalse();
        }

        await using var readContext = new AppDbContext(options);
        var savedOrder = await readContext.Orders
            .AsNoTracking()
            .Include(order => order.Payments)
            .SingleAsync(order => order.Id == orderId);
        var savedVariant = await readContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.StockMovements)
            .SingleAsync(variant => variant.Id == variantId);
        var savedCoupon = await readContext.Coupons.AsNoTracking().SingleAsync(coupon => coupon.Code == "FAIL10");
        savedOrder.Status.Should().Be(OrderStatus.Cancelled);
        savedOrder.ReservationExpiresAt.Should().BeNull();
        savedOrder.Payments.Should().ContainSingle(payment =>
            payment.Id == paymentId && payment.Status == PaymentStatus.Failed);
        savedVariant.Stock.Should().Be(5);
        savedVariant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation && movement.OrderId == orderId);
        savedCoupon.UsedCount.Should().Be(0);
        (await readContext.CouponUsages.AnyAsync(usage => usage.OrderId == orderId)).Should().BeFalse();
        var outboxTypes = await readContext.EmailOutbox
            .AsNoTracking()
            .Select(message => message.Type)
            .ToListAsync();
        outboxTypes.Should().ContainSingle(type => type == EmailOutboxMessageType.PaymentFailed);
        outboxTypes.Should().ContainSingle(type => type == EmailOutboxMessageType.OrderStatusChanged);
    }

    // Burada siparişe bağlı adres snapshot'ının kaynak adres sonradan değişse bile ayrı kayıtta tutulduğunu doğruluyorum.
    [Fact]
    public async Task Repository_Should_Persist_Order_Shipping_Address_Snapshot()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid orderId;
        Guid sourceAddressId;
        await using (var writeContext = new AppDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();
            var user = await SeedUserAsync(writeContext, "snapshot@example.com");
            var address = CreateAddress(user.Id, true);
            writeContext.Addresses.Add(address);
            await writeContext.SaveChangesAsync();
            var order = CreateOrder(user.Id, address.Id);
            order.SetShippingAddressSnapshot(address);
            writeContext.Orders.Add(order);
            await writeContext.SaveChangesAsync();
            orderId = order.Id;
            sourceAddressId = address.Id;
        }

        await using var readContext = new AppDbContext(options);
        var savedOrder = await readContext.Orders
            .AsNoTracking()
            .Include(order => order.AddressSnapshots)
            .SingleAsync(order => order.Id == orderId);

        savedOrder.ShippingAddressSnapshot.Should().NotBeNull();
        savedOrder.ShippingAddressSnapshot!.SourceAddressId.Should().Be(sourceAddressId);
        savedOrder.ShippingAddressSnapshot.FullAddress.Should().Be("Full Address");
    }

    // Burada ürün medya snapshot'larıyla kargo takip geçmişinin ilişkisel depoda birlikte kalıcı olduğunu doğruluyorum.
    [Fact]
    public async Task Repository_Should_Persist_Immutable_Order_Item_And_Shipment_Snapshots()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "order-media-shipment@example.com");
        var catalog = await SeedCatalogAsync(context, "snapshot", hasVariants: true);
        var utcNow = new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Utc);
        var order = CreateOrder(user.Id);
        order.AddItem(
            catalog.Product.Id,
            catalog.Variant.Id,
            catalog.Product.Title,
            catalog.Variant.Sku,
            10m,
            1,
            productUrlSnapshot: "product-snapshot",
            imageUrlSnapshot: "https://cdn.example.com/product-snapshot.jpg",
            imageAltSnapshot: "Snapshot image",
            variantNameSnapshot: "Renk",
            variantValueSnapshot: "Pudra");
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "snapshot_payment_key_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("snapshot_transaction_001");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.SetShipment("Carrier", "TRACK-123", "https://track.example.com/TRACK-123", utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        catalog.Variant.UpdateDetails(
            "Renk",
            "Siyah",
            catalog.Variant.Sku,
            catalog.Variant.Barcode,
            catalog.Variant.Material);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedOrder = await new OrderRepository(context).GetByIdAsync(order.Id);

        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Single().ProductUrlSnapshot.Should().Be("product-snapshot");
        savedOrder.Items.Single().ImageUrlSnapshot.Should().Be("https://cdn.example.com/product-snapshot.jpg");
        savedOrder.Items.Single().ImageAltSnapshot.Should().Be("Snapshot image");
        savedOrder.Items.Single().VariantNameSnapshot.Should().Be("Renk");
        savedOrder.Items.Single().VariantValueSnapshot.Should().Be("Pudra");
        savedOrder.ToDto().Items.Single().VariantName.Should().Be("Renk");
        savedOrder.ToDto().Items.Single().VariantValue.Should().Be("Pudra");
        savedOrder.ShippingCarrier.Should().Be("Carrier");
        savedOrder.TrackingNumber.Should().Be("TRACK-123");
        savedOrder.ShippedAt.Should().Be(utcNow.AddMinutes(3));
        savedOrder.DeliveredAt.Should().Be(utcNow.AddMinutes(4));
        savedOrder.ToDto().ShippedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        savedOrder.ToDto().DeliveredAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    // Burada kuponun aynı sipariş için iki kullanım kaydı oluşturmasına unique indeksin izin vermediğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_Coupon_Usage_For_The_Same_Order()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "coupon-usage@example.com");
        var coupon = new Coupon("SAVE10", CouponDiscountType.Percentage, 10m);
        var order = CreateOrder(user.Id);
        context.AddRange(coupon, order);
        await context.SaveChangesAsync();
        context.CouponUsages.AddRange(
            new CouponUsage(coupon.Id, user.Id, order.Id),
            new CouponUsage(coupon.Id, user.Id, order.Id));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada sipariş kaleminin ürün-varyant eşleşmesini bileşik yabancı anahtarın koruduğunu doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Order_Item_With_A_Mismatched_Product_And_Variant()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = new AppDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "order-item-match@example.com");
        var firstCatalog = await SeedCatalogAsync(context, "first");
        var secondCatalog = await SeedCatalogAsync(context, "second");
        var order = CreateOrder(user.Id);
        order.AddItem(
            secondCatalog.Product.Id,
            firstCatalog.Variant.Id,
            secondCatalog.Product.Title,
            firstCatalog.Variant.Sku,
            10m,
            1);
        order.EnsureItemsMatchSubTotal();
        context.Orders.Add(order);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada SQLite ilişkisel testleri için açık ve foreign key denetimli in-memory bağlantıyı oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        return connection;
    }

    // Burada aynı açık bağlantıyı paylaşacak DbContext seçeneklerini oluşturuyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    // Burada ilişkisel testler için geçerli kullanıcı kaydını oluşturuyorum.
    private static async Task<User> SeedUserAsync(AppDbContext context, string email)
    {
        var user = new User(email, "password-hash", "Order", "User");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    // Burada test adresinin kullanıcıya ait geçerli shipping adresini oluşturuyorum.
    private static Address CreateAddress(long userId, bool isDefault)
    {
        return new Address(
            userId,
            AddressType.Shipping,
            "Home",
            "Ada",
            "Yilmaz",
            "05000000000",
            "Izmir",
            "Konak",
            "Street 1",
            "Full Address",
            "35220",
            isDefault);
    }

    // Burada ilişkisel testlerde kullanılacak geçerli sipariş aggregate'ını oluşturuyorum.
    private static Order CreateOrder(long userId, Guid? addressId = null)
    {
        return new Order(userId, $"ORD-{Guid.NewGuid():N}"[..24], 10m, 0m, 0m, 0m, 10m, addressId);
    }

    // Burada sipariş kalemi eşleşme testi için iki farklı ürün ve varyantı kalıcı olarak oluşturuyorum.
    private static async Task<(Product Product, ProductVariant Variant)> SeedCatalogAsync(
        AppDbContext context,
        string suffix,
        bool hasVariants = false)
    {
        var product = new Product(
            $"Product {suffix}",
            $"product-{suffix}",
            $"PRODUCT-{suffix}",
            status: ProductStatus.Active,
            hasVariants: hasVariants);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var variant = new ProductVariant(
            product.Id,
            hasVariants ? "Renk" : $"Variant {suffix}",
            $"SKU-{suffix}",
            10m,
            5,
            value: hasVariants ? "Pudra" : null);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        return (product, variant);
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada adres varsayılanı entegrasyon testinin oturum kullanıcısı kimliğini hazırlıyorum.
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

    private sealed class SuccessfulPaymentGateway : IPaymentGateway
    {
        public PaymentProvider Provider => PaymentProvider.Fake;

        // Burada kalıcı ödeme akışını doğrulamak için başarılı sağlayıcı sonucunu üretiyorum.
        public Task<PaymentGatewayResult> ChargeAsync(
            PaymentGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentGatewayResult(
                true,
                "fake_persistence_payment_transaction_001",
                null));
        }
    }
}
