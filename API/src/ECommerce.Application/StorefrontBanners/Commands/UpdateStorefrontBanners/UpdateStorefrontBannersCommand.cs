using ECommerce.Application.StorefrontBanners.Dtos;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Commands.UpdateStorefrontBanners;

// Burada tüm storefront banner setini değiştirme isteğini tanımlıyorum.
public sealed record UpdateStorefrontBannersCommand(
    string? MainBannerImageUrl = null,
    IReadOnlyList<string>? AltBannerImageUrls = null) : IRequest<StorefrontBannersDto>;
