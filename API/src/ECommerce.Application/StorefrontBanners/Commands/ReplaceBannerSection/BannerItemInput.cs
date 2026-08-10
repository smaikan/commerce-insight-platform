using ECommerce.Domain.Enums;

namespace ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;

// Burada tek banner medya kaydının yönetimden gelen alanlarını taşıyorum.
public sealed record BannerItemInput(
    string Name,
    string Key,
    string MediaUrl,
    BannerMediaType MediaType,
    string? TargetUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    bool IsMain);
