using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IStorefrontBannerRepository
{
    // Burada storefront banner setini takip etmeden okuma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<StorefrontBanner>> GetAllAsync(CancellationToken cancellationToken = default);

    // Burada storefront banner setini tek kayıtta değiştirme sözleşmesini tanımlıyorum.
    Task ReplaceAsync(
        IReadOnlyCollection<StorefrontBanner> banners,
        CancellationToken cancellationToken = default);
}
