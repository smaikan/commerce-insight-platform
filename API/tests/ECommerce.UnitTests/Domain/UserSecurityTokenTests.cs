using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class UserSecurityTokenTests
{
    [Fact]
    public void Constructor_Should_Create_Usable_Security_Token()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserSecurityToken(
            1,
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            utcNow.AddHours(1),
            utcNow);

        token.Type.Should().Be(UserSecurityTokenType.PasswordReset);
        token.TokenHash.Should().Be("security-token-hash");
        token.CanBeUsed(utcNow).Should().BeTrue();
    }

    [Fact]
    public void Invalidate_Should_Prevent_Token_From_Being_Used()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserSecurityToken(
            1,
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            utcNow.AddHours(1),
            utcNow);

        token.Invalidate(utcNow.AddMinutes(1));

        token.InvalidatedAt.Should().Be(utcNow.AddMinutes(1));
        token.CanBeUsed(utcNow.AddMinutes(2)).Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_Should_Prevent_Token_From_Being_Used_Again()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserSecurityToken(
            1,
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            utcNow.AddHours(1),
            utcNow);

        token.MarkAsUsed(utcNow.AddMinutes(1));

        token.IsUsed().Should().BeTrue();
        token.CanBeUsed(utcNow.AddMinutes(2)).Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_Should_Reject_Expired_Token()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserSecurityToken(
            1,
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            utcNow.AddSeconds(1),
            utcNow);

        Action act = () => token.MarkAsUsed(utcNow.AddSeconds(2));

        act.Should().Throw<DomainException>();
    }
}
