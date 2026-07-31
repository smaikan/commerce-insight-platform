using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class CartPersistenceTests
{
    // Burada sepetin ürün ve varyant bilgileriyle kaydedilip takip edilmeden geri okunabildiğini doğruluyorum.
    [Fact]
    public async Task Repository_Should_RoundTrip_Cart_Graph_Without_Tracking()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid cartId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = await SeedUserAsync(seedContext, "roundtrip@example.com");
            var catalog = await SeedCatalogAsync(seedContext, "roundtrip");
            var cart = Cart.CreateForUser(user.Id);
            cart.AddItem(catalog.Product.Id, catalog.Variant.Id, 2, 125.50m);
            seedContext.Carts.Add(cart);
            await seedContext.SaveChangesAsync();
            cartId = cart.Id;
        }

        await using var readContext = new AppDbContext(options);
        var cartRepository = new CartRepository(readContext);

        var savedCart = await cartRepository.GetByIdAsync(cartId);

        savedCart.Should().NotBeNull();
        savedCart!.Items.Should().ContainSingle();
        savedCart.TotalQuantity.Should().Be(2);
        savedCart.SubTotal.Should().Be(251.00m);
        var savedItem = savedCart.Items.Single();
        savedItem.Product.Title.Should().Be("Product roundtrip");
        savedItem.ProductVariant.Name.Should().Be("Variant roundtrip");
        readContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    // Burada kullanıcı ve misafir owner sorgularının doğru sepeti bulup yalnız update sorgusunda takip açtığını doğruluyorum.
    [Fact]
    public async Task Repository_Should_Filter_Owner_And_Use_Expected_Tracking_Mode()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        long userId;
        Guid userCartId;
        Guid guestCartId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = await SeedUserAsync(seedContext, "owner@example.com");
            var catalog = await SeedCatalogAsync(seedContext, "owner");
            var seededUserCart = Cart.CreateForUser(user.Id);
            var seededGuestCart = Cart.CreateForGuest("guest-owner-session");
            seededGuestCart.AddItem(catalog.Product.Id, catalog.Variant.Id, 1, 75m);
            seedContext.Carts.AddRange(seededUserCart, seededGuestCart);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
            userCartId = seededUserCart.Id;
            guestCartId = seededGuestCart.Id;
        }

        await using var context = new AppDbContext(options);
        var cartRepository = new CartRepository(context);

        var userCart = await cartRepository.GetByOwnerAsync(CartOwner.ForUser(userId));

        userCart.Should().NotBeNull();
        userCart!.Id.Should().Be(userCartId);
        context.ChangeTracker.Entries().Should().BeEmpty();

        var guestCart = await cartRepository.GetByOwnerForUpdateAsync(
            CartOwner.ForGuest("  guest-owner-session  "));

        guestCart.Should().NotBeNull();
        guestCart!.Id.Should().Be(guestCartId);
        guestCart.Items.Should().ContainSingle();
        context.Entry(guestCart).State.Should().Be(EntityState.Unchanged);
        context.Entry(guestCart.Items.Single()).State.Should().Be(EntityState.Unchanged);
    }

    // Burada benzersiz kullanıcı indeksinin aynı kullanıcı için ikinci sepeti reddettiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_User_Carts()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "duplicate-user@example.com");
        context.Carts.AddRange(
            Cart.CreateForUser(user.Id),
            Cart.CreateForUser(user.Id));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada benzersiz session indeksinin aynı misafir oturumu için ikinci sepeti reddettiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_Guest_Carts()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Carts.AddRange(
            Cart.CreateForGuest("duplicate-session"),
            Cart.CreateForGuest("duplicate-session"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada sepetin sahipsiz veya aynı anda iki sahipli olmasını veritabanı kontrolünün engellediğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Enforce_Exactly_One_Cart_Owner()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = await SeedUserAsync(context, "xor-owner@example.com");
        long? noUserId = null;
        string? noSessionId = null;

        var noOwnerAct = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "Carts"
                 ("Id", "UserId", "SessionId", "ConcurrencyToken", "CreatedAt")
             VALUES
                 ({Guid.NewGuid()}, {noUserId}, {noSessionId}, {Guid.NewGuid()}, {DateTime.UtcNow})
             """);
        var twoOwnersAct = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "Carts"
                 ("Id", "UserId", "SessionId", "ConcurrencyToken", "CreatedAt")
             VALUES
                 ({Guid.NewGuid()}, {user.Id}, {"two-owner-session"}, {Guid.NewGuid()}, {DateTime.UtcNow})
             """);

        await noOwnerAct.Should().ThrowAsync<SqliteException>();
        await twoOwnersAct.Should().ThrowAsync<SqliteException>();
    }

    // Burada sepet satırında sıfır adet veya sıfır fiyatın veritabanı kontrollerinden geçemediğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_NonPositive_Quantity_And_UnitPrice()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var catalog = await SeedCatalogAsync(context, "checks");
        var cart = Cart.CreateForGuest("check-session");
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        var zeroQuantityAct = () => InsertCartItemAsync(
            context,
            cart.Id,
            catalog.Product.Id,
            catalog.Variant.Id,
            0,
            10m);
        var zeroPriceAct = () => InsertCartItemAsync(
            context,
            cart.Id,
            catalog.Product.Id,
            catalog.Variant.Id,
            1,
            0m);

        await zeroQuantityAct.Should().ThrowAsync<SqliteException>();
        await zeroPriceAct.Should().ThrowAsync<SqliteException>();
    }

    // Burada başka ürüne ait varyantın sepet satırına bağlanmasını bileşik yabancı anahtarın reddettiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Product_And_Variant_Mismatch()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var firstCatalog = await SeedCatalogAsync(context, "first");
        var secondCatalog = await SeedCatalogAsync(context, "second");
        var cart = Cart.CreateForGuest("mismatch-session");
        cart.AddItem(
            secondCatalog.Product.Id,
            firstCatalog.Variant.Id,
            1,
            25m);
        context.Carts.Add(cart);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada aggregate koleksiyonundan çıkarılan sepet satırının orphan delete ile veritabanından silindiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Delete_Removed_Cart_Item_As_Orphan()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid cartId;
        Guid removedItemId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var catalog = await SeedCatalogAsync(seedContext, "orphan");
            var cart = Cart.CreateForGuest("orphan-session");
            removedItemId = cart.AddItem(
                catalog.Product.Id,
                catalog.Variant.Id,
                1,
                40m).Id;
            seedContext.Carts.Add(cart);
            await seedContext.SaveChangesAsync();
            cartId = cart.Id;
        }

        await using (var updateContext = new AppDbContext(options))
        {
            var cartRepository = new CartRepository(updateContext);
            var cart = await cartRepository.GetByOwnerForUpdateAsync(
                CartOwner.ForGuest("orphan-session"));
            cart.Should().NotBeNull();
            cart!.RemoveItem(removedItemId);
            await updateContext.SaveChangesAsync();
        }

        await using var assertContext = new AppDbContext(options);
        (await assertContext.Carts.AnyAsync(cart => cart.Id == cartId)).Should().BeTrue();
        (await assertContext.CartItems.AnyAsync(item => item.Id == removedItemId)).Should().BeFalse();
    }

    // Burada sepet silindiğinde bağlı satırların cascade davranışıyla birlikte silindiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Cascade_Items_When_Cart_Is_Removed()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid cartId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var catalog = await SeedCatalogAsync(seedContext, "cascade");
            var cart = Cart.CreateForGuest("cascade-session");
            cart.AddItem(catalog.Product.Id, catalog.Variant.Id, 1, 60m);
            seedContext.Carts.Add(cart);
            await seedContext.SaveChangesAsync();
            cartId = cart.Id;
        }

        await using (var deleteContext = new AppDbContext(options))
        {
            var cartRepository = new CartRepository(deleteContext);
            var cart = await cartRepository.GetByOwnerForUpdateAsync(
                CartOwner.ForGuest("cascade-session"));
            cart.Should().NotBeNull();
            cartRepository.Remove(cart!);
            await deleteContext.SaveChangesAsync();
        }

        await using var assertContext = new AppDbContext(options);
        (await assertContext.Carts.AnyAsync(cart => cart.Id == cartId)).Should().BeFalse();
        (await assertContext.CartItems.AnyAsync(item => item.CartId == cartId)).Should().BeFalse();
    }

    // Burada iki ayrı DbContext'in aynı sepeti değiştirmesi durumunda ikinci kaydın concurrency hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task UnitOfWork_Should_Report_Concurrent_Cart_Update()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var catalog = await SeedCatalogAsync(seedContext, "concurrency");
            var cart = Cart.CreateForGuest("concurrency-session");
            cart.AddItem(catalog.Product.Id, catalog.Variant.Id, 1, 90m);
            seedContext.Carts.Add(cart);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new CartRepository(firstContext);
        var secondRepository = new CartRepository(secondContext);
        var firstCart = await firstRepository.GetByOwnerForUpdateAsync(
            CartOwner.ForGuest("concurrency-session"));
        var secondCart = await secondRepository.GetByOwnerForUpdateAsync(
            CartOwner.ForGuest("concurrency-session"));
        firstCart.Should().NotBeNull();
        secondCart.Should().NotBeNull();
        var cartItemId = firstCart!.Items.Single().Id;

        firstCart.ChangeItemQuantity(cartItemId, 2);
        await firstContext.SaveChangesAsync();
        secondCart!.ChangeItemQuantity(cartItemId, 3);
        var act = () => new UnitOfWork(secondContext).SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    // Burada SQLite ilişkisel testleri için yabancı anahtarları açık bir in-memory bağlantı oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        return connection;
    }

    // Burada aynı açık SQLite bağlantısını kullanacak DbContext seçeneklerini oluşturuyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    // Burada Cart persistence testleri için geçerli ve benzersiz bir kullanıcı kaydı hazırlıyorum.
    private static async Task<User> SeedUserAsync(
        AppDbContext context,
        string email)
    {
        var user = new User(email, "password-hash", "Cart", "User");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    // Burada test sepetlerinin bağlanacağı geçerli ürün ve varyant kayıtlarını hazırlıyorum.
    private static async Task<(Product Product, ProductVariant Variant)> SeedCatalogAsync(
        AppDbContext context,
        string suffix)
    {
        var normalizedSuffix = suffix.Trim().ToLowerInvariant();
        var product = new Product(
            $"Product {suffix}",
            $"product-{normalizedSuffix}",
            $"PRODUCT-{normalizedSuffix}");
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant(
            product.Id,
            $"Variant {suffix}",
            $"VARIANT-{normalizedSuffix}",
            100m,
            10);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        return (product, variant);
    }

    // Burada Domain doğrulamasını atlayıp veritabanı adet ve fiyat constraintlerini doğrudan sınayan satırı ekliyorum.
    private static Task<int> InsertCartItemAsync(
        AppDbContext context,
        Guid cartId,
        long productId,
        Guid productVariantId,
        int quantity,
        decimal unitPrice)
    {
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "CartItems"
                 ("Id", "CartId", "ProductId", "ProductVariantId", "Quantity", "UnitPrice", "CreatedAt")
             VALUES
                 ({Guid.NewGuid()}, {cartId}, {productId}, {productVariantId}, {quantity}, {unitPrice}, {DateTime.UtcNow})
             """);
    }
}
