using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class StorefrontBannerPersistenceTests
{
    // Burada banner setinin ekleme, güncelleme ve eksilen alanı kaldırma davranışını ilişkisel veritabanında doğruluyorum.
    [Fact]
    public async Task Replace_Should_Persist_Exactly_The_Requested_Banner_Slots()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var repository = new StorefrontBannerRepository(context);

        await repository.ReplaceAsync([
            new StorefrontBanner(StorefrontBannerSlot.Main, "https://cdn.example.com/main.jpg"),
            new StorefrontBanner(StorefrontBannerSlot.Alternate1, "https://cdn.example.com/alt-1.jpg"),
            new StorefrontBanner(StorefrontBannerSlot.Alternate2, "https://cdn.example.com/alt-2.jpg")
        ]);
        await context.SaveChangesAsync();

        await repository.ReplaceAsync([
            new StorefrontBanner(StorefrontBannerSlot.Main, "https://cdn.example.com/main-new.jpg"),
            new StorefrontBanner(StorefrontBannerSlot.Alternate1, "https://cdn.example.com/alt-new.jpg")
        ]);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var rows = await repository.GetAllAsync();
        rows.Should().HaveCount(2);
        rows.Select(item => item.Slot).Should().Equal(
            StorefrontBannerSlot.Main,
            StorefrontBannerSlot.Alternate1);
        rows[0].ImageUrl.Should().Be("https://cdn.example.com/main-new.jpg");
        rows[1].ImageUrl.Should().Be("https://cdn.example.com/alt-new.jpg");
    }

    // Burada koleksiyon görsel URL değerinin veri tabanına yazılıp okunabildiğini doğruluyorum.
    [Fact]
    public async Task Collection_Image_Url_Should_Roundtrip()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var collection = new Collection("Yaz", "yaz", imageUrl: "https://cdn.example.com/yaz.jpg");
        context.Collections.Add(collection);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedCollection = await context.Collections.AsNoTracking().SingleAsync();

        savedCollection.ImageUrl.Should().Be("https://cdn.example.com/yaz.jpg");
    }

    // Burada SQLite bağlantısını kullanan test DbContext'ini hazırlıyorum.
    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        return new AppDbContext(options);
    }
}
