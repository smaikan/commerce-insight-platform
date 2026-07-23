using ECommerce.Application.Auth.Commands.RegisterUser;
using ECommerce.Application.Auth.Commands.ResetPassword;
using ECommerce.Application.Users.Commands.ChangePassword;
using FluentAssertions;

namespace ECommerce.UnitTests.Application;

public sealed class AuthPasswordValidatorTests
{
    [Fact]
    public void RegisterUser_Should_Accept_Six_Character_Password()
    {
        var result = new RegisterUserCommandValidator().Validate(
            new RegisterUserCommand("user@example.com", "Ab1!xy", "User", "Test"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPassword_Should_Accept_Six_Character_Password()
    {
        var result = new ResetPasswordCommandValidator().Validate(
            new ResetPasswordCommand("reset-token", "Ab1!xy"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePassword_Should_Accept_Six_Character_New_Password()
    {
        var result = new ChangePasswordCommandValidator().Validate(
            new ChangePasswordCommand("Current123!", "Ab1!xy"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Password_Validators_Should_Reject_Five_Character_Passwords()
    {
        const string shortPassword = "A1!xy";

        new RegisterUserCommandValidator()
            .Validate(new RegisterUserCommand("user@example.com", shortPassword, "User", "Test"))
            .IsValid.Should().BeFalse();
        new ResetPasswordCommandValidator()
            .Validate(new ResetPasswordCommand("reset-token", shortPassword))
            .IsValid.Should().BeFalse();
        new ChangePasswordCommandValidator()
            .Validate(new ChangePasswordCommand("Current123!", shortPassword))
            .IsValid.Should().BeFalse();
    }
}
