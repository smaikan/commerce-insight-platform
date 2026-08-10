using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Interfaces;

public interface IStorefrontBannerRepository
{
    // Burada tek banner bölümünü aktiflik filtresiyle okuma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<StorefrontBanner>> GetSectionAsync(
        StorefrontBannerSection section,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    // Burada tek banner bölümünün kayıtlarını diğer bölümlere dokunmadan değiştirme sözleşmesini tanımlıyorum.
    Task ReplaceSectionAsync(
        StorefrontBannerSection section,
        IReadOnlyCollection<StorefrontBanner> banners,
        CancellationToken cancellationToken = default);
}
