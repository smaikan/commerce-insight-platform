using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders.Services;

public sealed class OrderCouponService
{
    private readonly ICouponRepository _couponRepository;
    private readonly IDateTimeProvider _clock;

    // Burada checkout kuponu için repository ve güvenilir UTC saat kaynağını hazırlıyorum.
    public OrderCouponService(ICouponRepository couponRepository, IDateTimeProvider clock)
    {
        _couponRepository = couponRepository;
        _clock = clock;
    }

    // Burada kupon kodunu takipli çözüp mevcut sepet toplamına uygulanabilir indirimi hesaplıyorum.
    public async Task<CheckoutCoupon?> ResolveForCheckoutAsync(
        string? couponCode,
        decimal subTotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return null;
        }

        var coupon = await _couponRepository.GetByCodeForUpdateAsync(couponCode, cancellationToken)
            ?? throw new ConflictException("Coupon was not found.");
        try
        {
            return new CheckoutCoupon(coupon, coupon.CalculateDiscount(subTotal, _clock.UtcNow));
        }
        catch (DomainException exception)
        {
            throw new ConflictException("Coupon cannot be applied to this order.", exception);
        }
    }

    // Burada başarılı checkout sonrasında kupon sayacını ve siparişe bağlı kullanım kaydını aynı transaction içinde oluşturuyorum.
    public async Task ConsumeAsync(
        CheckoutCoupon? checkoutCoupon,
        long userId,
        Order order,
        CancellationToken cancellationToken)
    {
        if (checkoutCoupon is null)
        {
            return;
        }

        var existingUsage = await _couponRepository.GetUsageForOrderForUpdateAsync(
            checkoutCoupon.Coupon.Id,
            order.Id,
            cancellationToken);
        if (existingUsage is not null)
        {
            throw new ConflictException("Coupon has already been consumed for this order.");
        }

        checkoutCoupon.Coupon.IncreaseUsedCount(_clock.UtcNow);
        await _couponRepository.AddUsageAsync(
            new CouponUsage(checkoutCoupon.Coupon.Id, userId, order.Id, _clock.UtcNow),
            cancellationToken);
    }

    // Burada ödeme öncesi iptal edilen siparişin kupon kullanımını sayaç ve kayıt seviyesinde geri alıyorum.
    public async Task ReleaseForCancellationAsync(Order order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(order.CouponCode))
        {
            return;
        }

        var usage = await _couponRepository.GetUsageByOrderForUpdateAsync(order.Id, cancellationToken);
        if (usage is null)
        {
            return;
        }

        var coupon = await _couponRepository.GetByIdForUpdateAsync(usage.CouponId, cancellationToken)
            ?? throw new ConflictException("Coupon linked to the order was not found.");
        coupon.DecreaseUsedCount();
        _couponRepository.RemoveUsage(usage);
    }
}

// Burada checkout'ta kilitli kupon aggregate'ı ve hesaplanan indirimi birlikte taşıyorum.
public sealed record CheckoutCoupon(Coupon Coupon, decimal DiscountTotal);
