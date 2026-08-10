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

    // Burada banner görsel URL değerinin zorunlu olduğunu doğruluyorum.
    [Fact]
    public void StorefrontBanner_Should_Reject_Empty_Image_Url()
    {
        var act = () => new StorefrontBanner(StorefrontBannerSlot.Main, " ");

        act.Should().Throw<DomainException>();
    }

    // Burada banner alanı ve URL değerinin temizlenmiş biçimde saklandığını doğruluyorum.
    [Fact]
    public void StorefrontBanner_Should_Store_Slot_And_Trim_Image_Url()
    {
        var banner = new StorefrontBanner(
            StorefrontBannerSlot.Alternate1,
            " https://cdn.example.com/alt-1.jpg ");

        banner.Slot.Should().Be(StorefrontBannerSlot.Alternate1);
        banner.ImageUrl.Should().Be("https://cdn.example.com/alt-1.jpg");
    }
}
