using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Constructor_Should_Create_Active_Customer_User_With_Normalized_Email()
    {
        var user = new User(
            "  SERHAT@example.COM  ",
            "hashed-password",
            " Serhat ",
            " Test ",
            " 5551112233 ");

        user.Email.Should().Be("serhat@example.com");
        user.PasswordHash.Should().Be("hashed-password");
        user.FirstName.Should().Be("Serhat");
        user.LastName.Should().Be("Test");
        user.FullName.Should().Be("Serhat Test");
        user.PhoneNumber.Should().Be("5551112233");
        user.Role.Should().Be(UserRole.Customer);
        user.Status.Should().Be(UserStatus.Active);
        user.SecurityVersion.Should().Be(1);
        user.CanLogin().Should().BeTrue();
        user.RefreshTokens.Should().BeEmpty();
        user.SecurityTokens.Should().BeEmpty();
    }

    [Fact]
    public void Active_User_Should_Login_Without_Email_Confirmation()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test");

        user.CanLogin().Should().BeTrue();
    }

    [Fact]
    public void ChangeEmail_Should_Invalidate_Existing_Access_Tokens()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test");
        var securityVersion = user.SecurityVersion;

        user.ChangeEmail("new@example.com");

        user.Email.Should().Be("new@example.com");
        user.SecurityVersion.Should().Be(securityVersion + 1);
    }

    [Fact]
    public void Constructor_Should_Set_PhoneNumber_To_Null_When_PhoneNumber_Is_Whitespace()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", "   ");

        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_Should_Set_PhoneNumber_To_Null_When_PhoneNumber_Is_Whitespace()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", "5551112233");

        user.UpdateProfile("Updated", "User", "   ");

        user.FirstName.Should().Be("Updated");
        user.LastName.Should().Be("User");
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void ChangePassword_Should_Update_Password_And_Changed_Date()
    {
        var user = new User("user@example.com", "old-hash", "User", "Test");
        var utcNow = DateTime.UtcNow;

        user.ChangePassword("new-hash", DateTime.UtcNow);

        user.PasswordHash.Should().Be("new-hash");
        user.PasswordChangedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_Should_Reject_Deleted_User()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test");

        user.MarkAsDeleted();

        Action act = user.Activate;

        act.Should().Throw<DomainException>();
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void Constructor_Should_Reject_Invalid_Email(string email)
    {
        Action act = () => new User(email, "hashed-password", "User", "Test");

        act.Should().Throw<DomainException>();
    }
}
