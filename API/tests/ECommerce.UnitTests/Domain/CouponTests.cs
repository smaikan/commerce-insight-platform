using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class CouponTests
{
    // Burada kupon kodunun kanonik bÃ¼yÃ¼k harfli biÃ§ime dÃ¶nÃ¼ÅŸtÃ¼rÃ¼ldÃ¼ÄŸÃ¼nÃ¼ ve yÃ¼zdesel indirimin hesaplandÄ±ÄŸÄ±nÄ± doÄŸruluyorum.
    [Fact]
    public void Constructor_And_CalculateDiscount_Should_Normalize_Code_And_Calculate_Percentage()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var coupon = new Coupon(" summer10 ", CouponDiscountType.Percentage, 10m, startsAt: now.AddDays(-1));

        var discount = coupon.CalculateDiscount(125.55m, now);

        coupon.Code.Should().Be("SUMMER10");
        discount.Should().Be(12.56m);
    }

    // Burada geÃ§ersiz yÃ¼zdesel indirim deÄŸerinin kupon oluÅŸturulurken reddedildiÄŸini doÄŸruluyorum.
    [Fact]
    public void Constructor_Should_Reject_Percentage_Greater_Than_One_Hundred()
    {
        Action act = () => new Coupon("TOO-MUCH", CouponDiscountType.Percentage, 100.01m);

        act.Should().Throw<DomainException>();
    }

    // Burada yönetim ve checkout kurallarının aynı kalması için desteklenmeyen kupon karakterlerini domain seviyesinde reddediyorum.
    [Fact]
    public void Constructor_Should_Reject_A_Code_With_Unsupported_Characters()
    {
        Action act = () => new Coupon("SUMMER 20!", CouponDiscountType.FixedAmount, 10m);

        act.Should().Throw<DomainException>();
    }

    // Burada sÃ¼resi geÃ§en veya minimum tutara ulaÅŸmayan kuponun hesaplama yapamadÄ±ÄŸÄ±nÄ± doÄŸruluyorum.
    [Fact]
    public void CalculateDiscount_Should_Reject_Expired_Or_Below_Minimum_Order()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var expiredCoupon = new Coupon("OLD", CouponDiscountType.FixedAmount, 10m, expiresAt: now.AddMinutes(-1));
        var minimumCoupon = new Coupon("MIN", CouponDiscountType.FixedAmount, 10m, minimumOrderAmount: 100m);

        Action expiredAct = () => expiredCoupon.CalculateDiscount(100m, now);
        Action minimumAct = () => minimumCoupon.CalculateDiscount(99m, now);

        expiredAct.Should().Throw<DomainException>();
        minimumAct.Should().Throw<DomainException>();
    }

    // Burada mevcut kullanÄ±m sayÄ±sÄ±nÄ±n altÄ±na limit indirilmesinin engellendiÄŸini doÄŸruluyorum.
    [Fact]
    public void Update_Should_Reject_Usage_Limit_Below_Current_Usage()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var coupon = new Coupon("LIMIT", CouponDiscountType.FixedAmount, 10m, usageLimit: 2);
        coupon.IncreaseUsedCount(now);

        Action act = () => coupon.Update(
            "LIMIT",
            CouponDiscountType.FixedAmount,
            10m,
            null,
            null,
            0,
            null,
            null);

        act.Should().Throw<DomainException>();
    }

    // Burada kupon kullanÄ±m kaydÄ±nÄ±n yalnÄ±z bir sipariÅŸe baÄŸlanabildiÄŸini doÄŸruluyorum.
    [Fact]
    public void CouponUsage_Should_Allow_One_Order_Assignment()
    {
        var usage = new CouponUsage(Guid.NewGuid(), 7);
        var orderId = Guid.NewGuid();
        usage.AssignToOrder(orderId);

        Action act = () => usage.AssignToOrder(Guid.NewGuid());

        usage.OrderId.Should().Be(orderId);
        act.Should().Throw<DomainException>();
    }
}
