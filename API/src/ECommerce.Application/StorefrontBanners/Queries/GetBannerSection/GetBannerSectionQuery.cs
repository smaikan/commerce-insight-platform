using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Queries.GetBannerSection;

// Burada belirli banner bölümünü aktif veya yönetim görünümüyle okuma isteğini tanımlıyorum.
public sealed record GetBannerSectionQuery(
    StorefrontBannerSection Section,
    bool ActiveOnly) : IRequest<BannerSectionDto>;
