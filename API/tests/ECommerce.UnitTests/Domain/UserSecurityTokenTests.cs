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
            Guid.NewGuid(),
            UserSecurityTokenType.EmailConfirmation,
            "security-token-hash",
            utcNow.AddHours(1));

        token.Type.Should().Be(UserSecurityTokenType.EmailConfirmation);
        token.TokenHash.Should().Be("security-token-hash");
        token.CanBeUsed(utcNow).Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_Should_Prevent_Token_From_Being_Used_Again()
    {
        var token = new UserSecurityToken(
            Guid.NewGuid(),
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            DateTime.UtcNow.AddHours(1));

        token.MarkAsUsed(DateTime.UtcNow.AddMinutes(1));

        token.IsUsed().Should().BeTrue();
        token.CanBeUsed(DateTime.UtcNow.AddMinutes(2)).Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_Should_Reject_Expired_Token()
    {
        var utcNow = DateTime.UtcNow;
        var token = new UserSecurityToken(
            Guid.NewGuid(),
            UserSecurityTokenType.PasswordReset,
            "security-token-hash",
            utcNow.AddSeconds(1));

        Action act = () => token.MarkAsUsed(utcNow.AddSeconds(2));

        act.Should().Throw<DomainException>();
    }
}
