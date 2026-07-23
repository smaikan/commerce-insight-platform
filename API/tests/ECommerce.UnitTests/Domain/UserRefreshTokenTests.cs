using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class UserRefreshTokenTests
{
    [Fact]
    public void Constructor_Should_Create_Active_Refresh_Token()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserRefreshToken(1, "refresh-token-hash", utcNow.AddDays(7), utcNow, "127.0.0.1");

        token.TokenHash.Should().Be("refresh-token-hash");
        token.CreatedByIp.Should().Be("127.0.0.1");
        token.IsActive(utcNow).Should().BeTrue();
        token.IsExpired(utcNow).Should().BeFalse();
        token.IsRevoked().Should().BeFalse();
    }

    [Fact]
    public void Revoke_Should_Mark_Token_As_Revoked()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserRefreshToken(1, "refresh-token-hash", utcNow.AddDays(7), utcNow);

        token.Revoke(DateTime.UtcNow.AddMinutes(1), "127.0.0.1", "new-refresh-token-hash");

        token.IsRevoked().Should().BeTrue();
        token.IsActive(utcNow.AddMinutes(2)).Should().BeFalse();
        token.RevokedByIp.Should().Be("127.0.0.1");
        token.ReplacedByTokenHash.Should().Be("new-refresh-token-hash");
    }

    [Fact]
    public void Revoke_Should_Reject_Already_Revoked_Token()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserRefreshToken(1, "refresh-token-hash", utcNow.AddDays(7), utcNow);

        token.Revoke(DateTime.UtcNow.AddMinutes(1));
        Action act = () => token.Revoke(DateTime.UtcNow.AddMinutes(2));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_Reject_Expired_Token()
    {
        var utcNow = DateTime.UtcNow;
        Action act = () => new UserRefreshToken(1, "refresh-token-hash", utcNow.AddMinutes(-1), utcNow);

        act.Should().Throw<DomainException>();
    }
}
