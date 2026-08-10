using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.StorefrontBanners.Dtos;

// Burada tek banner bölümünün sabit kimliği ile sıralı medya kayıtlarını taşıyorum.
public sealed record BannerSectionDto(
    string Name,
    string Key,
    IReadOnlyList<BannerItemDto> Items);

// Burada storefront ve yönetim ekranlarının kullandığı banner medya sözleşmesini tanımlıyorum.
public sealed record BannerItemDto(
    Guid Id,
    string Name,
    string Key,
    string MediaUrl,
    BannerMediaType MediaType,
    string? TargetUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    bool IsMain);

public static class BannerSectionDtoMapping
{
    // Burada banner kayıtlarını bölüm kimliği ve kararlı sıralamayla API sözleşmesine dönüştürüyorum.
    public static BannerSectionDto ToDto(
        this IEnumerable<StorefrontBanner> banners,
        StorefrontBannerSection section)
    {
        var metadata = GetMetadata(section);
        var items = banners
            .OrderByDescending(banner => banner.IsMain)
            .ThenBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.Key)
            .Select(banner => new BannerItemDto(
                banner.Id,
                banner.Name,
                banner.Key,
                banner.MediaUrl,
                banner.MediaType,
                banner.TargetUrl,
                banner.AltText,
                banner.DisplayOrder,
                banner.IsActive,
                banner.IsMain))
            .ToList();

        return new BannerSectionDto(metadata.Name, metadata.Key, items);
    }

    // Burada sabit banner bölümünün kullanıcı dostu adını ve kararlı anahtarını eşliyorum.
    private static (string Name, string Key) GetMetadata(StorefrontBannerSection section) => section switch
    {
        StorefrontBannerSection.Main => ("Main Banner", "main-banner"),
        StorefrontBannerSection.AltBanner1 => ("Alt Banner 1", "alt-banner-1"),
        StorefrontBannerSection.AltBanner2 => ("Alt Banner 2", "alt-banner-2"),
        StorefrontBannerSection.AltBanner3 => ("Alt Banner 3", "alt-banner-3"),
        StorefrontBannerSection.AltBanner4 => ("Alt Banner 4", "alt-banner-4"),
        StorefrontBannerSection.AltBanner5 => ("Alt Banner 5", "alt-banner-5"),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown banner section.")
    };
}
