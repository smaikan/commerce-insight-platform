using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Models;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class CatalogDeletionPersistenceTests
{
    // Burada soft delete sonrasında ürünün katalogdan gizlenip stok geçmişinin korunduğunu doğruluyorum.
    [Fact]
    public async Task Product_Soft_Delete_Should_Hide_Catalog_Graph_And_Preserve_Stock_History()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        long productId;
        Guid variantId;
        Guid imageId;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var product = CreateProduct("Protected", "protected", "PROTECTED-MAIN", "PROTECTED-STD");
            var variant = product.Variants.Single();
            variant.ApplyStockMovement(1, StockMovementType.ManualAdjustment, "Soft delete history test");
            var image = new ProductImage(product, "https://cdn.test/protected.jpg");
            product.Images.Add(image);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            productId = product.Id;
            variantId = variant.Id;
            imageId = image.Id;

            var trackedProduct = await new ProductRepository(context).GetByIdForDeletionAsync(productId);
            trackedProduct!.SoftDelete(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));
            await context.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        (await new ProductRepository(readContext).GetByIdAsync(productId)).Should().BeNull();
        var deletedProduct = await readContext.Products.SingleAsync(product => product.Id == productId);
        deletedProduct.Status.Should().Be(ProductStatus.Archived);
        deletedProduct.DeletedAtUtc.Should().NotBeNull();
        (await readContext.StockMovements.CountAsync(movement => movement.ProductVariantId == variantId)).Should().Be(1);
        (await new ProductVariantRepository(readContext).GetByIdAsync(variantId)).Should().BeNull();
        (await new ProductImageRepository(readContext).GetByIdAsync(imageId)).Should().BeNull();
        var adminList = await new ProductListReader(readContext).GetListAsync(new ProductListFilter(1, 20));
        adminList.Items.Should().BeEmpty();

        var replacement = CreateProduct("Replacement", "protected", "PROTECTED-MAIN", "REPLACEMENT-STD");
        readContext.Products.Add(replacement);
        await readContext.SaveChangesAsync();
        replacement.Id.Should().NotBe(productId);
    }

    // Burada marka, tür, koleksiyon ve etiket silinirken ürünün korunup yalnız referansların kaldırıldığını doğruluyorum.
    [Fact]
    public async Task Taxonomy_Deletion_Should_Preserve_Product_And_Remove_Only_References()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        long productId;
        Guid brandId;
        Guid typeId;
        Guid collectionId;
        Guid tagId;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var brand = new Brand("Brand", "brand");
            var productType = new ProductType("Type");
            var collection = new Collection("Collection", "collection");
            var tag = new Tag("Tag", "tag");
            var product = new Product("Product", "product", "PRODUCT-MAIN", productType.Id, brand.Id);
            product.Variants.Add(new ProductVariant(product, "Standard", "PRODUCT-STD", 100m, 0));
            context.AddRange(brand, productType, collection, tag, product);
            await context.SaveChangesAsync();
            context.ProductCollections.Add(new ProductCollection(product.Id, collection.Id));
            context.ProductTags.Add(new ProductTag(product.Id, tag.Id));
            await context.SaveChangesAsync();

            productId = product.Id;
            brandId = brand.Id;
            typeId = productType.Id;
            collectionId = collection.Id;
            tagId = tag.Id;
        }

        await using (var deleteContext = new AppDbContext(options))
        {
            deleteContext.Brands.Remove(await deleteContext.Brands.SingleAsync(item => item.Id == brandId));
            deleteContext.ProductTypes.Remove(await deleteContext.ProductTypes.SingleAsync(item => item.Id == typeId));
            deleteContext.Collections.Remove(await deleteContext.Collections.SingleAsync(item => item.Id == collectionId));
            deleteContext.Tags.Remove(await deleteContext.Tags.SingleAsync(item => item.Id == tagId));
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        var preservedProduct = await readContext.Products.SingleAsync(product => product.Id == productId);
        preservedProduct.BrandId.Should().BeNull();
        preservedProduct.TypeId.Should().BeNull();
        (await readContext.ProductCollections.CountAsync(item => item.ProductId == productId)).Should().Be(0);
        (await readContext.ProductTags.CountAsync(item => item.ProductId == productId)).Should().Be(0);
        (await readContext.ProductVariants.CountAsync(item => item.ProductId == productId)).Should().Be(1);
    }

    // Burada testlerin aynı açık SQLite bağlantısında çalışmasını sağlayan seçenekleri hazırlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

    // Burada bellek içi SQLite veritabanını test boyunca açık tutuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada silme testleri için tek varyantlı ürün grafiğini hazırlıyorum.
    private static Product CreateProduct(string title, string url, string mainSku, string variantSku)
    {
        var product = new Product(title, url, mainSku);
        product.Variants.Add(new ProductVariant(product, "Standard", variantSku, 100m, 0));
        return product;
    }
}
