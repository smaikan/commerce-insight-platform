using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ProductPopularityScorePersistenceTests
{
    // Burada ürünlerin varsayılan olarak en yüksek popülerlik puanından başlayarak listelendiğini doğruluyorum.
    [Fact]
    public async Task ProductRepository_Should_Order_By_Popularity_Score_By_Default()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var lowScoreProduct = CreateProduct("Low Score", "low-score", "LOW-SCORE");
        lowScoreProduct.IncreaseClickCount();
        var highScoreProduct = CreateProduct("High Score", "high-score", "HIGH-SCORE");
        highScoreProduct.IncreaseTotalPurchaseCount(1);
        context.Products.AddRange(lowScoreProduct, highScoreProduct);
        await context.SaveChangesAsync();

        var result = await new ProductRepository(context)
            .GetListAsync(new ProductListFilter(1, 20));

        result.Items.Select(product => product.Title)
            .Should().ContainInOrder("High Score", "Low Score");
    }

    // Burada sıralama testinde kullanılacak geçerli ürünü tek varyantıyla hazırlıyorum.
    private static Product CreateProduct(string title, string url, string sku)
    {
        var product = new Product(title, url);
        product.Variants.Add(new ProductVariant(product, "Standard", sku, 100m, 1));
        return product;
    }

    // Burada ilişkisel davranışı sınamak için açık SQLite bağlantısı oluşturuyorum.
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
            .Options;
    }
}
