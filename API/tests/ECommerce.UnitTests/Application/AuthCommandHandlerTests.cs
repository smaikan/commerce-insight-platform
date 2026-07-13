using ECommerce.Application.Auth.Commands.Login;
using ECommerce.Application.Auth.Commands.RefreshToken;
using ECommerce.Application.Auth.Commands.RegisterUser;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class AuthCommandHandlerTests
{
    [Fact]
    public async Task RegisterUser_Should_Create_User_With_Email_Confirmation_Token()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var authSettingsProvider = new FixedAuthSettingsProvider();
        User? createdUser = null;

        userRepository
            .Setup(repository => repository.EmailExistsAsync("user@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .Returns(Task.CompletedTask);

        passwordHasher
            .Setup(hasher => hasher.Hash("Password123!"))
            .Returns("hashed-password");

        randomTokenGenerator
            .Setup(generator => generator.GenerateToken())
            .Returns("email-token");

        tokenHasher
            .Setup(hasher => hasher.Hash("email-token"))
            .Returns("hashed-email-token");

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RegisterUserCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            randomTokenGenerator.Object,
            tokenHasher.Object,
            authSettingsProvider,
            dateTimeProvider,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RegisterUserCommand(" USER@example.com ", "Password123!", "User", "Test"),
            CancellationToken.None);

        result.User.Email.Should().Be("user@example.com");
        result.EmailConfirmationToken.Should().Be("email-token");
        result.EmailConfirmationTokenExpiresAt.Should().Be(dateTimeProvider.UtcNow.AddHours(24));
        createdUser.Should().NotBeNull();
        createdUser!.PasswordHash.Should().Be("hashed-password");
        createdUser.SecurityTokens.Should().ContainSingle(token => token.TokenHash == "hashed-email-token");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_Should_Create_Access_And_Refresh_Tokens_When_Credentials_Are_Valid()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var authSettingsProvider = new FixedAuthSettingsProvider();
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);

        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasher
            .Setup(hasher => hasher.Verify("Password123!", "hashed-password"))
            .Returns(true);

        jwtTokenGenerator
            .Setup(generator => generator.GenerateAccessToken(user))
            .Returns(new AccessTokenResult("access-token", dateTimeProvider.UtcNow.AddMinutes(15)));

        randomTokenGenerator
            .Setup(generator => generator.GenerateToken())
            .Returns("refresh-token");

        tokenHasher
            .Setup(hasher => hasher.Hash("refresh-token"))
            .Returns("hashed-refresh-token");

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new LoginCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            jwtTokenGenerator.Object,
            randomTokenGenerator.Object,
            tokenHasher.Object,
            authSettingsProvider,
            dateTimeProvider,
            unitOfWork.Object);

        var result = await handler.Handle(
            new LoginCommand("user@example.com", "Password123!", "127.0.0.1"),
            CancellationToken.None);

        result.Tokens.AccessToken.Should().Be("access-token");
        result.Tokens.RefreshToken.Should().Be("refresh-token");
        result.Tokens.RefreshTokenExpiresAt.Should().Be(dateTimeProvider.UtcNow.AddDays(14));
        user.RefreshTokens.Should().ContainSingle(token => token.TokenHash == "hashed-refresh-token");
        user.LastLoginAt.Should().Be(dateTimeProvider.UtcNow);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_Should_Record_Failed_Attempt_When_Password_Is_Invalid()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var authSettingsProvider = new FixedAuthSettingsProvider(new AuthSettings { MaxFailedAccessAttempts = 1 });
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);

        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasher
            .Setup(hasher => hasher.Verify("wrong-password", "hashed-password"))
            .Returns(false);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new LoginCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            jwtTokenGenerator.Object,
            randomTokenGenerator.Object,
            tokenHasher.Object,
            authSettingsProvider,
            dateTimeProvider,
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new LoginCommand("user@example.com", "wrong-password"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        user.AccessFailedCount.Should().Be(1);
        user.LockoutEndAt.Should().Be(dateTimeProvider.UtcNow.AddMinutes(15));
        user.Status.Should().Be(ECommerce.Domain.Enums.UserStatus.Active);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_Should_Revoke_Old_Token_And_Create_New_Token()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var authSettingsProvider = new FixedAuthSettingsProvider();
        var user = new User("user@example.com", "hashed-password", "User", "Test", emailConfirmed: true);
        user.RefreshTokens.Add(new UserRefreshToken(user.Id, "old-refresh-hash", dateTimeProvider.UtcNow.AddDays(1)));

        tokenHasher
            .Setup(hasher => hasher.Hash("old-refresh-token"))
            .Returns("old-refresh-hash");

        tokenHasher
            .Setup(hasher => hasher.Hash("new-refresh-token"))
            .Returns("new-refresh-hash");

        userRepository
            .Setup(repository => repository.GetByRefreshTokenHashForUpdateAsync("old-refresh-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        jwtTokenGenerator
            .Setup(generator => generator.GenerateAccessToken(user))
            .Returns(new AccessTokenResult("new-access-token", dateTimeProvider.UtcNow.AddMinutes(15)));

        randomTokenGenerator
            .Setup(generator => generator.GenerateToken())
            .Returns("new-refresh-token");

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RefreshTokenCommandHandler(
            userRepository.Object,
            jwtTokenGenerator.Object,
            randomTokenGenerator.Object,
            tokenHasher.Object,
            authSettingsProvider,
            dateTimeProvider,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefreshTokenCommand("old-refresh-token", "127.0.0.1"),
            CancellationToken.None);

        result.Tokens.AccessToken.Should().Be("new-access-token");
        result.Tokens.RefreshToken.Should().Be("new-refresh-token");
        user.RefreshTokens.Should().Contain(token => token.TokenHash == "new-refresh-hash");
        user.RefreshTokens.Single(token => token.TokenHash == "old-refresh-hash").IsRevoked().Should().BeTrue();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FixedAuthSettingsProvider : IAuthSettingsProvider
    {
        private readonly AuthSettings _settings;

        public FixedAuthSettingsProvider()
            : this(new AuthSettings())
        {
        }

        public FixedAuthSettingsProvider(AuthSettings settings)
        {
            _settings = settings;
        }

        public AuthSettings GetSettings()
        {
            return _settings;
        }
    }
}
