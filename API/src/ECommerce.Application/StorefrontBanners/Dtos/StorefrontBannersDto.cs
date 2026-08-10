using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.StorefrontBanners.Dtos;

// Burada storefront banner okuma sözleşmesinin alanlarını tanımlıyorum.
public sealed record StorefrontBannersDto(
    string? MainBannerImageUrl,
    IReadOnlyList<string> AltBannerImageUrls);

public static class StorefrontBannersDtoMapping
{
    // Burada sabit banner alanlarını storefront sözleşmesinde sıralı URL listesine dönüştürüyorum.
    public static StorefrontBannersDto ToDto(this IEnumerable<StorefrontBanner> banners)
    {
        var bannerList = banners.ToList();
        var mainImageUrl = bannerList
            .FirstOrDefault(banner => banner.Slot == StorefrontBannerSlot.Main)
            ?.ImageUrl;
        var alternateImageUrls = bannerList
            .Where(banner => banner.Slot is >= StorefrontBannerSlot.Alternate1 and <= StorefrontBannerSlot.Alternate5)
            .OrderBy(banner => banner.Slot)
            .Select(banner => banner.ImageUrl)
            .ToList();

        return new StorefrontBannersDto(mainImageUrl, alternateImageUrls);
    }
}
