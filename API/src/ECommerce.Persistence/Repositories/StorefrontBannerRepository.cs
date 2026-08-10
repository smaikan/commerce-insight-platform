using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class StorefrontBannerRepository : IStorefrontBannerRepository
{
    private readonly AppDbContext _context;

    // Burada storefront banner sorguları için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public StorefrontBannerRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada banner setini sabit alan sırasıyla ve takip etmeden getiriyorum.
    public async Task<IReadOnlyList<StorefrontBanner>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.StorefrontBanners
            .AsNoTracking()
            .OrderBy(banner => banner.Slot)
            .ToListAsync(cancellationToken);
    }

    // Burada mevcut banner satırlarını güncelleyip kaldırılan ve eklenen alanları aynı değişiklik setine alıyorum.
    public async Task ReplaceAsync(
        IReadOnlyCollection<StorefrontBanner> banners,
        CancellationToken cancellationToken = default)
    {
        var desiredBySlot = banners.ToDictionary(banner => banner.Slot);
        var existingBanners = await _context.StorefrontBanners.ToListAsync(cancellationToken);

        foreach (var existingBanner in existingBanners)
        {
            if (desiredBySlot.Remove(existingBanner.Slot, out var desiredBanner))
            {
                existingBanner.UpdateImageUrl(desiredBanner.ImageUrl);
            }
            else
            {
                _context.StorefrontBanners.Remove(existingBanner);
            }
        }

        if (desiredBySlot.Count > 0)
        {
            await _context.StorefrontBanners.AddRangeAsync(desiredBySlot.Values, cancellationToken);
        }
    }
}
