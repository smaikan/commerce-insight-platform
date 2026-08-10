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
    // Burada bir bölüm değiştirilirken diğer bölümün korunup anahtar kimliği ve aktiflik filtresinin çalıştığını doğruluyorum.
    [Fact]
    public async Task ReplaceSection_Should_Update_Only_Target_Section_And_Preserve_Key_Identity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var repository = new StorefrontBannerRepository(context);

        await repository.ReplaceSectionAsync(StorefrontBannerSection.Main, [
            CreateBanner(StorefrontBannerSection.Main, "hero", 0, isMain: true),
            CreateBanner(StorefrontBannerSection.Main, "secondary", 1, isActive: false)
        ]);
        await repository.ReplaceSectionAsync(StorefrontBannerSection.AltBanner1, [
            CreateBanner(StorefrontBannerSection.AltBanner1, "alt-one", 0)
        ]);
        await context.SaveChangesAsync();
        var heroId = (await repository.GetSectionAsync(StorefrontBannerSection.Main, activeOnly: false))
            .Single(item => item.Key == "hero").Id;

        await repository.ReplaceSectionAsync(StorefrontBannerSection.Main, [
            CreateBanner(StorefrontBannerSection.Main, "new-main", 4, isMain: true),
            CreateBanner(StorefrontBannerSection.Main, "hero", 0, isActive: false)
        ]);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var mainRows = await repository.GetSectionAsync(StorefrontBannerSection.Main, activeOnly: false);
        var altRows = await repository.GetSectionAsync(StorefrontBannerSection.AltBanner1, activeOnly: false);
        var activeMainRows = await repository.GetSectionAsync(StorefrontBannerSection.Main, activeOnly: true);

        mainRows.Select(item => item.Key).Should().Equal("new-main", "hero");
        mainRows.Single(item => item.Key == "hero").Id.Should().Be(heroId);
        altRows.Should().ContainSingle(item => item.Key == "alt-one");
        activeMainRows.Should().ContainSingle(item => item.Key == "new-main");
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

    // Burada persistence testleri için geçerli banner kayıtları hazırlıyorum.
    private static StorefrontBanner CreateBanner(
        StorefrontBannerSection section,
        string key,
        int displayOrder,
        bool isActive = true,
        bool isMain = false) =>
        new(
            section,
            $"Banner {key}",
            key,
            $"https://cdn.example.com/{key}.jpg",
            BannerMediaType.Image,
            "/collections/summer",
            $"Banner {key}",
            displayOrder,
            isActive,
            isMain);
}
