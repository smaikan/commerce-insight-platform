using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class BrandImagePersistenceTests
{
    // Burada marka görsel URL değerinin ilişkisel veri tabanına yazılıp okunabildiğini doğruluyorum.
    [Fact]
    public async Task Brand_Image_Url_Should_Roundtrip_As_Optional_Value()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var brand = new Brand(
            "Serantis",
            "serantis",
            imageUrl: "https://cdn.example.com/brands/serantis.jpg");
        context.Brands.Add(brand);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedBrand = await context.Brands.AsNoTracking().SingleAsync();

        savedBrand.ImageUrl.Should().Be("https://cdn.example.com/brands/serantis.jpg");
    }
}
