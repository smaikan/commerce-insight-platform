using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface ICouponRepository
{
    // Burada yeni kuponu veritabanÄ± takibine ekleme sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);

    // Burada kuponu kimliÄŸiyle takip etmeden okuma sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada kuponu koduyla takip etmeden okuma sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    // Burada kuponu checkout sırasında sayaç güncellemesi için koduyla takipli getirme sözleşmesini tanımlıyorum.
    Task<Coupon?> GetByCodeForUpdateAsync(string code, CancellationToken cancellationToken = default);

    // Burada kuponu gÃ¼ncelleme amacÄ±yla takipli getirme sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task<Coupon?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada kuponlarÄ± sayfalama ve isteÄŸe baÄŸlÄ± aktiflik filtresiyle okuma sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task<PagedResult<Coupon>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    // Burada kupon kodunun baÅŸka bir kuponda kullanÄ±lÄ±p kullanÄ±lmadÄ±ÄŸÄ±nÄ± denetleme sÃ¶zleÅŸmesini tanÄ±mlÄ±yorum.
    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedCouponId = null,
        CancellationToken cancellationToken = default);

    // Burada kupon kullanım kaydını aynı sipariş için tekrar oluşturmamak üzere takipli getirme sözleşmesini tanımlıyorum.
    Task<CouponUsage?> GetUsageForOrderForUpdateAsync(
        Guid couponId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    // Burada iptal akışı için siparişe bağlı kupon kullanımını kupon kodundan bağımsız olarak takipli getiriyorum.
    Task<CouponUsage?> GetUsageByOrderForUpdateAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    // Burada kupon kullanım kaydını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddUsageAsync(CouponUsage usage, CancellationToken cancellationToken = default);

    // Burada iptal edilen siparişin kupon kullanım kaydını silmeye hazırlama sözleşmesini tanımlıyorum.
    void RemoveUsage(CouponUsage usage);
}
