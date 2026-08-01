using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class RelationalBehaviorTests
{
    // Burada türü olmayan ürünün koleksiyon ilişkisiyle kaydedilebildiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Save_Product_Without_Type_And_With_Collection()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var collection = new Collection("Summer", "summer");
        var product = new Product("Type Free Product", "type-free-product", "TYPE-FREE-MAIN");
        product.ProductCollections.Add(new ProductCollection(product, collection.Id));
        context.AddRange(collection, product);

        await context.SaveChangesAsync();

        var savedProduct = await context.Products
            .Include(item => item.ProductCollections)
            .SingleAsync(item => item.Id == product.Id);
        savedProduct.TypeId.Should().BeNull();
        savedProduct.ProductCollections.Should().ContainSingle(
            relation => relation.CollectionId == collection.Id);
    }

    // Burada aynı ürün için birden fazla ana görsel kaydını veritabanında engelliyorum.
    [Fact]
    public async Task Database_Should_Reject_Multiple_Main_Images_For_One_Product()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType("Shoes");
        var product = new Product("Runner", "runner", "RUNNER-MAIN", type.Id);
        context.AddRange(type, product);
        context.ProductImages.AddRange(
            new ProductImage(product, "https://cdn.test/one.jpg", isMain: true),
            new ProductImage(product, "https://cdn.test/two.jpg", isMain: true));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada eşzamanlı varyant stok güncellemesinin concurrency hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task UnitOfWork_Should_Report_Concurrent_Stock_Update()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);

        Guid variantId;
        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var type = new ProductType("Shoes");
            var product = new Product("Runner", "runner", "RUNNER-STOCK-MAIN", type.Id);
            var variant = new ProductVariant(product, "Size 42", "RUN-42", 100, 10);
            seedContext.AddRange(type, product, variant);
            await seedContext.SaveChangesAsync();
            variantId = variant.Id;
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstVariant = await firstContext.ProductVariants.SingleAsync(item => item.Id == variantId);
        var secondVariant = await secondContext.ProductVariants.SingleAsync(item => item.Id == variantId);

        firstVariant.ApplyStockMovement(
            -2,
            StockMovementType.ManualAdjustment,
            "First concurrent stock adjustment");
        await firstContext.SaveChangesAsync();

        secondVariant.ApplyStockMovement(
            -4,
            StockMovementType.ManualAdjustment,
            "Second concurrent stock adjustment");
        var unitOfWork = new UnitOfWork(secondContext);
        var act = () => unitOfWork.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    // Burada etiket repository'sinin istenen sayfa ve toplam kayıt bilgisini döndürdüğünü doğruluyorum.
    [Fact]
    public async Task TagRepository_Should_Return_Requested_Page_And_Total_Count()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Tags.AddRange(Enumerable.Range(1, 25).Select(index =>
            new Tag($"Tag {index:D2}", $"tag-{index:D2}")));
        await context.SaveChangesAsync();

        var repository = new TagRepository(context);
        var result = await repository.GetListAsync(pageNumber: 2, pageSize: 10);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(2);
    }

    // Burada eşzamanlı ürün güncellemesinin concurrency hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task UnitOfWork_Should_Report_Concurrent_Product_Update()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        long productId;
        await using (var seed = new AppDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            var type = new ProductType("Shoes");
            var product = new Product("Runner", "runner", "RUNNER-CONCURRENCY-MAIN", type.Id);
            seed.AddRange(type, product);
            await seed.SaveChangesAsync();
            productId = product.Id;
        }
        await using var first = new AppDbContext(options);
        await using var second = new AppDbContext(options);
        var firstProduct = await first.Products.SingleAsync(item => item.Id == productId);
        var secondProduct = await second.Products.SingleAsync(item => item.Id == productId);
        firstProduct.UpdateBasics("Runner One", "runner-one", null, 0, null, null);
        await first.SaveChangesAsync();
        secondProduct.UpdateBasics("Runner Two", "runner-two", null, 0, null, null);

        var act = () => new UnitOfWork(second).SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    // Burada eşzamanlı kullanıcı güncellemesinin concurrency hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task UnitOfWork_Should_Report_Concurrent_User_Update()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        long userId;
        await using (var seed = new AppDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            var user = new User("user@example.com", "hash", "User", "Test");
            seed.Users.Add(user);
            await seed.SaveChangesAsync();
            userId = user.Id;
        }
        await using var first = new AppDbContext(options);
        await using var second = new AppDbContext(options);
        var firstUser = await first.Users.SingleAsync(item => item.Id == userId);
        var secondUser = await second.Users.SingleAsync(item => item.Id == userId);
        firstUser.UpdateProfile("First", "User");
        await first.SaveChangesAsync();
        secondUser.UpdateProfile("Second", "User");

        var act = () => new UnitOfWork(second).SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    // Burada ürün repository'sinin arama ve durum filtrelerini birlikte uyguladığını doğruluyorum.
    [Fact]
    public async Task ProductRepository_Should_Filter_Search_And_Status()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var type = new ProductType("Shoes");
        var blueProduct = new Product(
            "Blue Runner",
            "blue-runner",
            "BLUE-RUNNER-MAIN",
            type.Id,
            status: ECommerce.Domain.Enums.ProductStatus.Active);
        blueProduct.Variants.Add(new ProductVariant(
            blueProduct, "Blue / Standard", "BLUE-STD", 100, 5));
        var redProduct = new Product(
            "Red Runner",
            "red-runner",
            "RED-RUNNER-MAIN",
            type.Id,
            status: ECommerce.Domain.Enums.ProductStatus.Draft);
        redProduct.Variants.Add(new ProductVariant(
            redProduct, "Red / Standard", "RED-STD", 110, 3));
        context.AddRange(type, blueProduct, redProduct);
        await context.SaveChangesAsync();

        var result = await new ProductRepository(context).GetListAsync(new ProductListFilter(
            1, 20, Search: "Blue", Status: ECommerce.Domain.Enums.ProductStatus.Active));

        result.Items.Should().ContainSingle(item => item.Title == "Blue Runner");
        result.Items.Single().Variants.Should().ContainSingle(variant =>
            variant.Name == "Blue / Standard" && variant.Sku == "BLUE-STD");
    }

    // Burada ilişkisel testler için açık SQLite bağlantısı oluşturuyorum.
<<<<<<< HEAD
=======
    // Burada katalog read-model sorgusunun mevcut API DTO sözleşmesini ve filtre davranışını koruduğunu doğruluyorum.
    [Fact]
    public async Task ProductListReader_Should_Return_Filtered_ProductDto_With_Variants()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType("Shoes");
        var matchingProduct = new Product(
            "Blue Runner",
            "blue-runner",
            "BLUE-RUNNER-DTO",
            type.Id,
            status: ProductStatus.Active);
        matchingProduct.Variants.Add(new ProductVariant(
            matchingProduct,
            "Blue / Standard",
            "BLUE-DTO-STD",
            100m,
            5));
        var excludedProduct = new Product(
            "Red Runner",
            "red-runner",
            "RED-RUNNER-DTO",
            type.Id,
            status: ProductStatus.Draft);
        context.AddRange(type, matchingProduct, excludedProduct);
        await context.SaveChangesAsync();

        var result = await new ProductListReader(context).GetListAsync(
            new ProductListFilter(1, 20, Search: "Blue", Status: ProductStatus.Active));

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Blue Runner");
        result.Items[0].Id.Should().Be("P00001");
        result.Items[0].TypeName.Should().Be("Shoes");
        result.Items[0].Variants.Should().ContainSingle(variant =>
            variant.ProductId == "P00001" &&
            variant.Name == "Blue / Standard" &&
            variant.Sku == "BLUE-DTO-STD");
    }

    [Fact]
    public async Task ProductRepository_Should_Resolve_Published_Current_And_Legacy_Urls_Only()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var published = new Product(
            "Published Product",
            "published-product",
            "PUBLISHED-MAIN",
            status: ProductStatus.Active);
        published.Variants.Add(new ProductVariant(published, "Standard", "PUBLISHED-STD", 100m, 2));
        published.Images.Add(new ProductImage(published, "https://cdn.test/published.jpg"));
        published.UrlRedirects.Add(new ProductUrlRedirect(published, "legacy-product"));

        var draft = new Product("Draft Product", "draft-product", "DRAFT-MAIN", status: ProductStatus.Draft);
        draft.Variants.Add(new ProductVariant(draft, "Standard", "DRAFT-STD", 80m, 2));
        context.AddRange(published, draft);
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);
        var legacyResult = await repository.GetPublishedByUrlAsync("legacy-product");
        var draftResult = await repository.GetPublishedByUrlAsync("draft-product");
        var seoIndex = await repository.GetPublishedSeoIndexAsync(1, 100);

        legacyResult.Should().NotBeNull();
        legacyResult!.Url.Should().Be("published-product");
        legacyResult.Images.Should().ContainSingle(image => image.AltText == "Published Product");
        draftResult.Should().BeNull();
        seoIndex.Items.Should().ContainSingle(item => item.Url == "published-product");
    }

>>>>>>> 91f816d (123)
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada test DbContext ayarlarını açık SQLite bağlantısına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine)
            .Options;
    }
}
