using ECommerce.Application.Auth.Commands.Login;
using ECommerce.Application.Auth.Commands.RefreshToken;
using ECommerce.Application.Auth.Commands.RegisterUser;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class AuthCommandHandlerTests
{
    // Burada kayıt sırasında kullanıcının ve hoş geldin e-postası kuyruğunun birlikte oluşturulduğunu doğruluyorum.
    [Fact]
    public async Task RegisterUser_Should_Create_User_Without_Email_Confirmation()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var emailOutboxRepository = new Mock<IEmailOutboxRepository>();
        var utcNow = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
        User? createdUser = null;
        EmailOutboxMessage? queuedEmail = null;

        userRepository
            .Setup(repository => repository.EmailExistsAsync("user@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user.WithId(1))
            .Returns(Task.CompletedTask);

        passwordHasher
            .Setup(hasher => hasher.Hash("Password123!"))
            .Returns("hashed-password");

        emailOutboxRepository
            .Setup(repository => repository.AddAsync(It.IsAny<EmailOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailOutboxMessage, CancellationToken>((message, _) => queuedEmail = message)
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RegisterUserCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            emailOutboxRepository.Object,
            new FixedDateTimeProvider(utcNow),
            unitOfWork.Object);

        var result = await handler.Handle(
            new RegisterUserCommand(" USER@example.com ", "Password123!", "User", "Test"),
            CancellationToken.None);

        result.User.Email.Should().Be("user@example.com");
        createdUser.Should().NotBeNull();
        createdUser!.PasswordHash.Should().Be("hashed-password");
        createdUser.SecurityTokens.Should().BeEmpty();
        queuedEmail.Should().NotBeNull();
        queuedEmail!.Type.Should().Be(EmailOutboxMessageType.Welcome);
        queuedEmail.Email.Should().Be("user@example.com");
        queuedEmail.RecipientName.Should().Be("User Test");
        queuedEmail.CreatedAt.Should().Be(utcNow);
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
        var user = new User("user@example.com", "hashed-password", "User", "Test").WithId(1);
        var existingRefreshToken = new UserRefreshToken(
            user.Id,
            "existing-refresh-token-hash",
            dateTimeProvider.UtcNow.AddDays(7),
            dateTimeProvider.UtcNow.AddDays(-1));
        user.RefreshTokens.Add(existingRefreshToken);

        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasher
            .Setup(hasher => hasher.Verify("Password123!", "hashed-password"))
            .Returns(true);

        jwtTokenGenerator
            .Setup(generator => generator.GenerateAccessToken(user, It.IsAny<Guid>()))
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
        user.RefreshTokens.Should().HaveCount(2);
        user.RefreshTokens.Should().Contain(token => token.TokenHash == "hashed-refresh-token");
        existingRefreshToken.IsRevoked().Should().BeFalse();
        user.LastLoginAt.Should().Be(dateTimeProvider.UtcNow);
        userRepository.Verify(repository => repository.AddRefreshTokenAsync(
            It.Is<UserRefreshToken>(token => token.TokenHash == "hashed-refresh-token"),
            It.IsAny<CancellationToken>()), Times.Once);
        userRepository.Verify(repository => repository.GetActiveRefreshTokensForUpdateAsync(
            It.IsAny<long>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_Should_Reject_Invalid_Password_Without_Changing_User()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var authSettingsProvider = new FixedAuthSettingsProvider();
        var user = new User("user@example.com", "hashed-password", "User", "Test").WithId(1);

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

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        user.CanLogin().Should().BeTrue();
        user.Status.Should().Be(ECommerce.Domain.Enums.UserStatus.Active);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
        var user = new User("user@example.com", "hashed-password", "User", "Test").WithId(1);
        user.RefreshTokens.Add(new UserRefreshToken(
            user.Id,
            "old-refresh-hash",
            dateTimeProvider.UtcNow.AddDays(1),
            dateTimeProvider.UtcNow));

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
            .Setup(generator => generator.GenerateAccessToken(user, It.IsAny<Guid>()))
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
        userRepository.Verify(repository => repository.AddRefreshTokenAsync(
            It.Is<UserRefreshToken>(token => token.TokenHash == "new-refresh-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_Should_Perform_Dummy_Hash_Verification_When_User_Does_Not_Exist()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var handler = new LoginCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<IRandomTokenGenerator>(),
            Mock.Of<ITokenHasher>(),
            new FixedAuthSettingsProvider(),
            new FixedDateTimeProvider(DateTime.UtcNow),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(
            new LoginCommand("missing@example.com", "Password123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        passwordHasher.Verify(hasher => hasher.Verify(
            "Password123!",
            It.Is<string>(hash => hash.StartsWith("PBKDF2-SHA256", StringComparison.Ordinal))), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_Should_Revoke_All_Sessions_When_Reused_Token_Is_Detected()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var user = new User("user@example.com", "password-hash", "User", "Test").WithId(1);
        var reusedToken = new UserRefreshToken(user.Id, "reused-hash", utcNow.AddDays(1), utcNow.AddHours(-1));
        reusedToken.Revoke(utcNow.AddMinutes(-1), replacedByTokenHash: "replacement-hash");
        var activeToken = new UserRefreshToken(user.Id, "active-hash", utcNow.AddDays(1), utcNow.AddMinutes(-30));
        user.RefreshTokens.Add(reusedToken);
        var repository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(hasher => hasher.Hash("reused-token")).Returns("reused-hash");
        repository
            .Setup(item => item.GetByRefreshTokenHashForUpdateAsync("reused-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository
            .Setup(item => item.GetActiveRefreshTokensForUpdateAsync(user.Id, utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeToken]);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<IRandomTokenGenerator>(),
            tokenHasher.Object,
            new FixedAuthSettingsProvider(),
            new FixedDateTimeProvider(utcNow),
            unitOfWork.Object);

        var act = () => handler.Handle(new RefreshTokenCommand("reused-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        activeToken.RevokedAt.Should().Be(utcNow);
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
