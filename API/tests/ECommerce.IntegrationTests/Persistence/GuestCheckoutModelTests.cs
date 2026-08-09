using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class GuestCheckoutModelTests
{
    // Burada guest sahiplik alanlarının nullable ve kupon üyelik alanının zorunlu varsayılan olduğunu model seviyesinde doğruluyorum.
    [Fact]
    public void Model_Should_Expose_Nullable_Guest_Ownership_And_MemberOnly_Coupon()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.UserId))!.IsNullable.Should().BeTrue();
        context.Model.FindEntityType(typeof(ReturnRequest))!.FindProperty(nameof(ReturnRequest.UserId))!.IsNullable.Should().BeTrue();
        context.Model.FindEntityType(typeof(CouponUsage))!.FindProperty(nameof(CouponUsage.UserId))!.IsNullable.Should().BeTrue();
        context.Model.FindEntityType(typeof(Coupon))!.FindProperty(nameof(Coupon.IsMemberOnly))!.IsNullable.Should().BeFalse();
    }

    // Burada guest token tablolarında ham token alanı bulunmadığını ve hash alanlarının sabit uzunlukta olduğunu doğruluyorum.
    [Fact]
    public void Model_Should_Store_Only_Hashed_Guest_Tokens()
    {
        using var context = CreateContext();
        var session = context.Model.FindEntityType(typeof(GuestOrderSession))!;
        var magicLink = context.Model.FindEntityType(typeof(GuestOrderMagicLink))!;

        session.FindProperty("RawToken").Should().BeNull();
        session.FindProperty(nameof(GuestOrderSession.TokenHash))!.GetMaxLength().Should().Be(64);
        magicLink.FindProperty("RawToken").Should().BeNull();
        magicLink.FindProperty(nameof(GuestOrderMagicLink.TokenHash))!.GetMaxLength().Should().Be(64);
    }

    // Burada model metadatası için geçici SQLite DbContext oluşturuyorum.
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AppDbContext(options);
    }
}
