using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class StorefrontBannerRepository : IStorefrontBannerRepository
{
    private readonly AppDbContext _context;

    // Burada banner bölüm sorguları için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public StorefrontBannerRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada tek bölümün banner kayıtlarını aktiflik filtresi ve görünüm sırasıyla getiriyorum.
    public async Task<IReadOnlyList<StorefrontBanner>> GetSectionAsync(
        StorefrontBannerSection section,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StorefrontBanners
            .AsNoTracking()
            .Where(banner => banner.Section == section);

        if (activeOnly)
        {
            query = query.Where(banner => banner.IsActive);
        }

        return await query
            .OrderByDescending(banner => banner.IsMain)
            .ThenBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.Key)
            .ToListAsync(cancellationToken);
    }

    // Burada yalnız hedef bölümün kayıtlarını anahtar üzerinden güncelliyor, ekliyor veya kaldırıyorum.
    public async Task ReplaceSectionAsync(
        StorefrontBannerSection section,
        IReadOnlyCollection<StorefrontBanner> banners,
        CancellationToken cancellationToken = default)
    {
        var desiredByKey = banners.ToDictionary(banner => banner.Key, StringComparer.OrdinalIgnoreCase);
        var existingBanners = await _context.StorefrontBanners
            .Where(banner => banner.Section == section)
            .ToListAsync(cancellationToken);

        foreach (var existingBanner in existingBanners)
        {
            if (desiredByKey.Remove(existingBanner.Key, out var desiredBanner))
            {
                existingBanner.Update(
                    desiredBanner.Name,
                    desiredBanner.MediaUrl,
                    desiredBanner.MediaType,
                    desiredBanner.TargetUrl,
                    desiredBanner.AltText,
                    desiredBanner.DisplayOrder,
                    desiredBanner.IsActive,
                    desiredBanner.IsMain);
            }
            else
            {
                _context.StorefrontBanners.Remove(existingBanner);
            }
        }

        if (desiredByKey.Count > 0)
        {
            await _context.StorefrontBanners.AddRangeAsync(desiredByKey.Values, cancellationToken);
        }
    }
}
