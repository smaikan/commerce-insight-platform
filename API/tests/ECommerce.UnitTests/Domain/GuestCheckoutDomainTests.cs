using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class GuestCheckoutDomainTests
{
    // Burada kuponun varsayılan olarak guest kullanıma açık olduğunu ve yönetici güncellemesiyle üyeye özel yapılabildiğini doğruluyorum.
    [Fact]
    public void Coupon_Should_Default_To_Guest_Eligible_And_Allow_MemberOnly_Update()
    {
        var coupon = new Coupon("WELCOME", CouponDiscountType.Percentage, 10m);

        coupon.IsMemberOnly.Should().BeFalse();
        coupon.Update("WELCOME", CouponDiscountType.Percentage, 10m, null, null, null, null, null, true);

        coupon.IsMemberOnly.Should().BeTrue();
    }

    // Burada guest siparişin kullanıcı kimliği olmadan zorunlu müşteri, shipping ve billing snapshot'larını sakladığını doğruluyorum.
    [Fact]
    public void Guest_Order_Should_Store_Customer_And_Both_Address_Snapshots()
    {
        var order = new Order(
            null,
            "ORD-GUEST-1",
            100m,
            0m,
            0m,
            20m,
            120m,
            shippingMethodId: Guid.NewGuid(),
            shippingMethodName: "Standart");

        order.SetCustomerSnapshot("Ada", "Lovelace", "ADA@EXAMPLE.COM", "+905551112233");
        order.SetGuestShippingAddressSnapshot(
            "Ev", "Ada", "Lovelace", "+905551112233", "İstanbul", "Kadıköy", "Mahalle", "Örnek Sokak 1", "34000");
        order.SetBillingAddressSnapshot(
            null, "Fatura", "Ada", "Lovelace", "+905551112233", "İstanbul", "Kadıköy", "Mahalle", "Örnek Sokak 1", "34000");

        order.UserId.Should().BeNull();
        order.CustomerSnapshot!.Email.Should().Be("ada@example.com");
        order.ShippingAddressSnapshot!.SourceAddressId.Should().BeNull();
        order.BillingAddressSnapshot!.Type.Should().Be(AddressType.Billing);
        order.AddressSnapshots.Should().HaveCount(2);
    }

    // Burada guest kupon kullanımının kullanıcı olmadan siparişe bağlanıp claim sırasında kullanıcı alabilmesini doğruluyorum.
    [Fact]
    public void CouponUsage_Should_Allow_Guest_And_Later_Claim()
    {
        var usage = new CouponUsage(Guid.NewGuid(), null, Guid.NewGuid(), DateTime.UtcNow);

        usage.UserId.Should().BeNull();
        usage.AssignToUser(42);

        usage.UserId.Should().Be(42);
    }

    // Burada magic linkin otuz dakikalık pencere içinde yalnız bir kez tüketilebildiğini doğruluyorum.
    [Fact]
    public void MagicLink_Should_Be_One_Time_And_Expiring()
    {
        var now = DateTime.UtcNow;
        var link = new GuestOrderMagicLink(
            Guid.NewGuid(), new string('A', 64), new string('B', 64), now, now.AddMinutes(30));

        link.IsActiveAt(now).Should().BeTrue();
        link.Consume(now.AddMinutes(1));

        link.IsActiveAt(now.AddMinutes(2)).Should().BeFalse();
        var action = () => link.Consume(now.AddMinutes(2));
        action.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }

    // Burada aynı session-sipariş grant'inin claim dışı yeni doğrulanmış linkte benzersizliği bozmadan yeniden açılabildiğini doğruluyorum.
    [Fact]
    public void Revoked_Access_Grant_Should_Be_Reactivateable()
    {
        var now = DateTime.UtcNow;
        var grant = new GuestOrderAccessGrant(Guid.NewGuid(), Guid.NewGuid(), now);

        grant.Revoke(now.AddMinutes(1));
        grant.Reactivate(now.AddMinutes(2));

        grant.RevokedAt.Should().BeNull();
        grant.GrantedAt.Should().Be(now.AddMinutes(2));
    }
}



