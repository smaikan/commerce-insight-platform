using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class StorefrontBannerAndCollectionImageTests
{
    // Burada koleksiyon görsel URL değerinin temizlenip kaldırılabildiğini doğruluyorum.
    [Fact]
    public void Collection_Should_Store_And_Clear_Image_Url()
    {
        var collection = new Collection("Yaz", "yaz", imageUrl: " https://cdn.example.com/yaz.jpg ");

        collection.ImageUrl.Should().Be("https://cdn.example.com/yaz.jpg");

        collection.SetImageUrl(" ");

        collection.ImageUrl.Should().BeNull();
    }

    // Burada banner medya URL değerinin zorunlu olduğunu doğruluyorum.
    [Fact]
    public void StorefrontBanner_Should_Reject_Empty_Media_Url()
    {
        var act = () => CreateBanner(StorefrontBannerSection.Main, mediaUrl: " ", isMain: true);

        act.Should().Throw<DomainException>();
    }

    // Burada banner metinlerinin kırpılıp anahtarın küçük harfe normalleştirildiğini doğruluyorum.
    [Fact]
    public void StorefrontBanner_Should_Normalize_Fields()
    {
        var banner = CreateBanner(
            StorefrontBannerSection.Main,
            name: " Main Hero ",
            key: "SUMMER_HERO",
            mediaUrl: " https://cdn.example.com/hero.mp4 ",
            mediaType: BannerMediaType.Video,
            isMain: true);

        banner.Name.Should().Be("Main Hero");
        banner.Key.Should().Be("summer_hero");
        banner.MediaUrl.Should().Be("https://cdn.example.com/hero.mp4");
        banner.MediaType.Should().Be(BannerMediaType.Video);
        banner.IsMain.Should().BeTrue();
    }

    // Burada alt banner bölümündeki bir kaydın ana banner seçilemediğini doğruluyorum.
    [Fact]
    public void StorefrontBanner_Should_Reject_Main_Flag_In_Alternate_Section()
    {
        var act = () => CreateBanner(StorefrontBannerSection.AltBanner1, isMain: true);

        act.Should().Throw<DomainException>();
    }

    // Burada domain testleri için geçerli varsayılan alanlarla banner kaydı oluşturuyorum.
    private static StorefrontBanner CreateBanner(
        StorefrontBannerSection section,
        string name = "Banner",
        string key = "banner-key",
        string mediaUrl = "https://cdn.example.com/banner.jpg",
        BannerMediaType mediaType = BannerMediaType.Image,
        bool isMain = false) =>
        new(
            section,
            name,
            key,
            mediaUrl,
            mediaType,
            "/collections/summer",
            "Banner alt text",
            0,
            true,
            isMain);
}
