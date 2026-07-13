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
        var token = new UserRefreshToken(Guid.NewGuid(), "refresh-token-hash", utcNow.AddDays(7), "127.0.0.1");

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
        var token = new UserRefreshToken(Guid.NewGuid(), "refresh-token-hash", utcNow.AddDays(7));

        token.Revoke(DateTime.UtcNow.AddMinutes(1), "127.0.0.1", "new-refresh-token-hash");

        token.IsRevoked().Should().BeTrue();
        token.IsActive(utcNow.AddMinutes(2)).Should().BeFalse();
        token.RevokedByIp.Should().Be("127.0.0.1");
        token.ReplacedByTokenHash.Should().Be("new-refresh-token-hash");
    }

    [Fact]
    public void Revoke_Should_Reject_Already_Revoked_Token()
    {
        var token = new UserRefreshToken(Guid.NewGuid(), "refresh-token-hash", DateTime.UtcNow.AddDays(7));

        token.Revoke(DateTime.UtcNow.AddMinutes(1));
        Action act = () => token.Revoke(DateTime.UtcNow.AddMinutes(2));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_Reject_Expired_Token()
    {
        Action act = () => new UserRefreshToken(Guid.NewGuid(), "refresh-token-hash", DateTime.UtcNow.AddMinutes(-1));

        act.Should().Throw<DomainException>();
    }
}
