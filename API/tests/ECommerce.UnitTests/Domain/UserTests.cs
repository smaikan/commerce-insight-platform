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
        user.EmailConfirmed.Should().BeFalse();
        user.RefreshTokens.Should().BeEmpty();
        user.SecurityTokens.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmEmail_Should_Allow_Login_When_User_Is_Active()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test");
        var utcNow = DateTime.UtcNow;

        user.ConfirmEmail();

        user.EmailConfirmed.Should().BeTrue();
        user.CanLogin(utcNow).Should().BeTrue();
    }

    [Fact]
    public void ChangeEmail_Should_Reset_Email_Confirmation()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);

        user.ChangeEmail("new@example.com");

        user.Email.Should().Be("new@example.com");
        user.EmailConfirmed.Should().BeFalse();
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
    public void RecordFailedLogin_Should_Lock_User_When_Attempt_Limit_Is_Reached()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);
        var utcNow = DateTime.UtcNow;

        user.RecordFailedLogin(2, TimeSpan.FromMinutes(15), utcNow);
        user.RecordFailedLogin(2, TimeSpan.FromMinutes(15), utcNow);

        user.Status.Should().Be(UserStatus.Active);
        user.AccessFailedCount.Should().Be(2);
        user.LockoutEndAt.Should().NotBeNull();
        user.IsLocked(utcNow).Should().BeTrue();
        user.CanLogin(utcNow).Should().BeFalse();
    }

    [Fact]
    public void CanLogin_Should_Return_True_When_Lockout_Has_Expired()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);
        var utcNow = DateTime.UtcNow;

        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15), utcNow);

        user.IsLocked(utcNow.AddMinutes(16)).Should().BeFalse();
        user.CanLogin(utcNow.AddMinutes(16)).Should().BeTrue();
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_Reset_Failed_Attempts()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);
        var utcNow = DateTime.UtcNow;

        user.RecordFailedLogin(3, TimeSpan.FromMinutes(15), utcNow);
        user.RecordSuccessfulLogin(utcNow.AddMinutes(1));

        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndAt.Should().BeNull();
        user.LastLoginAt.Should().NotBeNull();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_Reject_Login_Before_Lockout_Ends()
    {
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);
        var utcNow = DateTime.UtcNow;

        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15), utcNow);

        Action act = () => user.RecordSuccessfulLogin(utcNow.AddMinutes(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangePassword_Should_Reset_Lockout_And_Update_Password_Changed_Date()
    {
        var user = new User("user@example.com", "old-hash", "User", "Test", emailConfirmed: true);
        var utcNow = DateTime.UtcNow;

        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15), utcNow);
        user.ChangePassword("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndAt.Should().BeNull();
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
