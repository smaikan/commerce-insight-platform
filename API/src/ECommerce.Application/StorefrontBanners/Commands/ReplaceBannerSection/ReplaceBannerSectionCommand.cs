using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;

// Burada tek bir banner bölümünün medya setini atomik değiştirme isteğini tanımlıyorum.
public sealed record ReplaceBannerSectionCommand(
    StorefrontBannerSection Section,
    IReadOnlyList<BannerItemInput> Items) : IRequest<BannerSectionDto>;
