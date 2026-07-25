using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class CouponRepository : ICouponRepository
{
    private readonly AppDbContext _context;

    // Burada kupon sorgu ve deÄŸiÅŸiklikleri iÃ§in aynÄ± istek kapsamÄ±ndaki DbContext'i hazÄ±rlÄ±yorum.
    public CouponRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni kuponu veritabanÄ± takibine ekliyorum.
    public async Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await _context.Coupons.AddAsync(coupon, cancellationToken);
    }

    // Burada kuponu kimliÄŸiyle okuma amacÄ±yla takip etmeden getiriyorum.
    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
    }

    // Burada normalleÅŸtirilmiÅŸ kupon koduyla okuma amacÄ±yla takip etmeden kaydÄ± getiriyorum.
    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);
        return _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(coupon => coupon.Code == normalizedCode, cancellationToken);
    }

    // Burada kuponu checkout içinde kullanım sayacını güncellemek üzere takipli getiriyorum.
    public Task<Coupon?> GetByCodeForUpdateAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);
        return _context.Coupons
            .FirstOrDefaultAsync(coupon => coupon.Code == normalizedCode, cancellationToken);
    }

    // Burada kuponu gÃ¼ncelleme iÃ§in takipli ÅŸekilde getiriyorum.
    public Task<Coupon?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Coupons
            .FirstOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
    }

    // Burada kuponlarÄ± kod sÄ±rasÄ±yla sayfalÄ± ve isteÄŸe baÄŸlÄ± aktiflik filtresiyle getiriyorum.
    public async Task<PagedResult<Coupon>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Coupon> query = _context.Coupons.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(coupon => coupon.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((pageNumber - 1) * pageSize);
        var items = await query
            .OrderBy(coupon => coupon.Code)
            .ThenBy(coupon => coupon.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Coupon>(items, pageNumber, pageSize, totalCount);
    }

    // Burada normalleÅŸtirilmiÅŸ kodun baÅŸka bir kupon tarafÄ±ndan kullanÄ±lÄ±p kullanÄ±lmadÄ±ÄŸÄ±nÄ± denetliyorum.
    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedCouponId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);
        return _context.Coupons.AnyAsync(
            coupon => coupon.Code == normalizedCode &&
                      (!excludedCouponId.HasValue || coupon.Id != excludedCouponId.Value),
            cancellationToken);
    }

    // Burada aynı sipariş için daha önce oluşturulmuş kupon kullanımını takipli getiriyorum.
    public Task<CouponUsage?> GetUsageForOrderForUpdateAsync(
        Guid couponId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _context.CouponUsages.FirstOrDefaultAsync(
            usage => usage.CouponId == couponId && usage.OrderId == orderId,
            cancellationToken);
    }

    // Burada siparişin kullanım kaydını kod değişikliklerinden etkilenmeden takipli olarak getiriyorum.
    public Task<CouponUsage?> GetUsageByOrderForUpdateAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _context.CouponUsages.FirstOrDefaultAsync(
            usage => usage.OrderId == orderId,
            cancellationToken);
    }

    // Burada yeni kupon kullanım kaydını veritabanı takibine ekliyorum.
    public async Task AddUsageAsync(CouponUsage usage, CancellationToken cancellationToken = default)
    {
        await _context.CouponUsages.AddAsync(usage, cancellationToken);
    }

    // Burada iptal edilen siparişin kupon kullanım kaydını silinmek üzere işaretliyorum.
    public void RemoveUsage(CouponUsage usage)
    {
        _context.CouponUsages.Remove(usage);
    }

    // Burada repository sorgularÄ±nda kullanÄ±lan kupon kodunu domain kanonik formatÄ±na getiriyorum.
    private static string NormalizeCode(string code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim().ToUpperInvariant();
    }
}
