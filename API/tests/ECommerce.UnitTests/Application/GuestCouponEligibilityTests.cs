using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class GuestCouponEligibilityTests
{
    // Burada üyeye özel kuponun indirim hesabından önce tam coupon_members_only hatasıyla reddedildiğini doğruluyorum.
    [Fact]
    public async Task MemberOnly_Coupon_Should_Return_Dedicated_Guest_Conflict()
    {
        var coupon = new Coupon(
            "MEMBER10", CouponDiscountType.Percentage, 10m, isMemberOnly: true);
        var repository = new Mock<ICouponRepository>();
        repository.Setup(candidate => candidate.GetByCodeForUpdateAsync(
                "MEMBER10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        var service = new OrderCouponService(repository.Object, new FixedClock());

        var action = () => service.ResolveForCheckoutAsync(
            "MEMBER10", 100m, true, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<CouponMembersOnlyException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.ErrorCode.Should().Be("coupon_members_only");
    }

    // Burada test için değişmez UTC zaman sağlayıcısı oluşturuyorum.
    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
    }
}
