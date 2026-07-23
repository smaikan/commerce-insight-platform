using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Commands.ChangePassword;
using ECommerce.Application.Users.Commands.ChangeEmail;
using ECommerce.Application.Users.Commands.LogoutAllSessions;
using ECommerce.Application.Users.Commands.UpdateProfile;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class UserAccountCommandHandlerTests
{
    [Fact]
    public async Task ChangePassword_Should_Revoke_All_Sessions_And_Increment_SecurityVersion()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var user = new User("user@example.com", "old-password-hash", "User", "Test").WithId(1);
        var refreshToken = new UserRefreshToken(
            user.Id,
            "refresh-token-hash",
            utcNow.AddDays(1),
            utcNow.AddHours(-1));
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        userRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.GetActiveRefreshTokensForUpdateAsync(user.Id, utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([refreshToken]);
        passwordHasher.Setup(hasher => hasher.Verify("CurrentPassword123!", user.PasswordHash)).Returns(true);
        passwordHasher.Setup(hasher => hasher.Hash("NewPassword123!")).Returns("new-password-hash");
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var securityVersion = user.SecurityVersion;
        var handler = new ChangePasswordCommandHandler(
            userRepository.Object,
            new FixedCurrentUserService(user.Id),
            passwordHasher.Object,
            new FixedDateTimeProvider(utcNow),
            unitOfWork.Object);

        await handler.Handle(
            new ChangePasswordCommand("CurrentPassword123!", "NewPassword123!"),
            CancellationToken.None);

        user.PasswordHash.Should().Be("new-password-hash");
        user.SecurityVersion.Should().Be(securityVersion + 1);
        refreshToken.RevokedAt.Should().Be(utcNow);
    }

    [Fact]
    public async Task ChangePassword_Should_Reject_Invalid_Current_Password()
    {
        var user = new User("user@example.com", "old-password-hash", "User", "Test").WithId(1);
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        userRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);
        var handler = new ChangePasswordCommandHandler(
            userRepository.Object,
            new FixedCurrentUserService(user.Id),
            passwordHasher.Object,
            new FixedDateTimeProvider(DateTime.UtcNow),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(
            new ChangePasswordCommand("WrongPassword!", "NewPassword123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LogoutAllSessions_Should_Revoke_Tokens_And_Invalidate_Access_Tokens()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var user = new User("user@example.com", "password-hash", "User", "Test").WithId(1);
        var token = new UserRefreshToken(user.Id, "token-hash", utcNow.AddDays(1), utcNow.AddHours(-1));
        var repository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(item => item.GetByIdForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repository
            .Setup(item => item.GetActiveRefreshTokensForUpdateAsync(user.Id, utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var securityVersion = user.SecurityVersion;
        var handler = new LogoutAllSessionsCommandHandler(
            repository.Object,
            new FixedCurrentUserService(user.Id),
            new FixedDateTimeProvider(utcNow),
            unitOfWork.Object);

        await handler.Handle(new LogoutAllSessionsCommand(), CancellationToken.None);

        token.RevokedAt.Should().Be(utcNow);
        user.SecurityVersion.Should().Be(securityVersion + 1);
    }

    [Fact]
    public async Task UpdateProfile_Should_Return_Safe_Profile_Data()
    {
        var user = new User("user@example.com", "password-hash", "User", "Test").WithId(1);
        var repository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(item => item.GetByIdForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new UpdateProfileCommandHandler(
            repository.Object,
            new FixedCurrentUserService(user.Id),
            unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProfileCommand("Updated", "Name", "5551112233"),
            CancellationToken.None);

        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("Name");
        result.PhoneNumber.Should().Be("5551112233");
    }

    [Fact]
    public async Task ChangeEmail_Should_Require_Password_And_Revoke_All_Sessions()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var user = new User("user@example.com", "password-hash", "User", "Test").WithId(1);
        var token = new UserRefreshToken(user.Id, "token-hash", utcNow.AddDays(1), utcNow.AddHours(-1));
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(item => item.GetByIdForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repository.Setup(item => item.EmailExistsAsync("new@example.com", user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(item => item.GetActiveRefreshTokensForUpdateAsync(user.Id, utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        passwordHasher.Setup(item => item.Verify("Current123!", user.PasswordHash)).Returns(true);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new ChangeEmailCommandHandler(repository.Object, new FixedCurrentUserService(user.Id),
            passwordHasher.Object, new FixedDateTimeProvider(utcNow), unitOfWork.Object);

        var result = await handler.Handle(new ChangeEmailCommand("Current123!", "new@example.com"), CancellationToken.None);

        result.Email.Should().Be("new@example.com");
        token.RevokedAt.Should().Be(utcNow);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FixedCurrentUserService : ICurrentUserService
    {
        public FixedCurrentUserService(long userId) => UserId = userId;
        public long? UserId { get; }
    }
}
