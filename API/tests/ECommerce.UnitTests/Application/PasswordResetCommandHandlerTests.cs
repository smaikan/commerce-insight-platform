using ECommerce.Application.Auth.Commands.CreatePasswordResetToken;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PasswordResetCommandHandlerTests
{
    // Burada bulunmayan kullanıcı için e-posta kuyruğu kaydı oluşmadığını doğruluyorum.
    [Fact]
    public async Task Request_Should_Return_Silently_When_Email_Does_Not_Exist()
    {
        var userRepository = new Mock<IUserRepository>();
        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(userRepository, outboxRepository, unitOfWork);

        await handler.Handle(new CreatePasswordResetTokenCommand("missing@example.com"), CancellationToken.None);

        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        outboxRepository.Verify(repository => repository.AddAsync(
            It.IsAny<EmailOutboxMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada eski tokenın iptal edilip parola sıfırlama e-postasının kuyruğa alındığını doğruluyorum.
    [Fact]
    public async Task Request_Should_Invalidate_Previous_Token_And_Queue_Email()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var user = new User("user@example.com", "password-hash", "User", "Test").WithId(1);
        var previousToken = new UserSecurityToken(
            user.Id,
            UserSecurityTokenType.PasswordReset,
            "old-token-hash",
            utcNow.AddMinutes(20),
            utcNow.AddMinutes(-1));
        user.SecurityTokens.Add(previousToken);
        var userRepository = new Mock<IUserRepository>();
        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        userRepository
            .Setup(repository => repository.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.GetActiveSecurityTokensForUpdateAsync(
                user.Id,
                UserSecurityTokenType.PasswordReset,
                utcNow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([previousToken]);
        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        EmailOutboxMessage? queuedMessage = null;
        outboxRepository.Setup(repository => repository.AddAsync(
                It.IsAny<EmailOutboxMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailOutboxMessage, CancellationToken>((message, _) => queuedMessage = message)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(userRepository, outboxRepository, unitOfWork, utcNow);

        await handler.Handle(new CreatePasswordResetTokenCommand("user@example.com"), CancellationToken.None);

        previousToken.InvalidatedAt.Should().Be(utcNow);
        user.SecurityTokens.Should().HaveCount(2);
        queuedMessage.Should().NotBeNull();
        queuedMessage!.Email.Should().Be(user.Email);
        queuedMessage.ProtectedToken.Should().Be("protected-reset-token");
        queuedMessage.ExpiresAt.Should().Be(utcNow.AddMinutes(30));
        queuedMessage.Type.Should().Be(EmailOutboxMessageType.PasswordReset);
    }

    // Burada parola sıfırlama handler'ını sabit test bağımlılıklarıyla oluşturuyorum.
    private static CreatePasswordResetTokenCommandHandler CreateHandler(
        Mock<IUserRepository> userRepository,
        Mock<IEmailOutboxRepository> outboxRepository,
        Mock<IUnitOfWork> unitOfWork,
        DateTime? utcNow = null)
    {
        var randomTokenGenerator = new Mock<IRandomTokenGenerator>();
        var tokenHasher = new Mock<ITokenHasher>();
        randomTokenGenerator.Setup(generator => generator.GenerateToken()).Returns("raw-reset-token");
        tokenHasher.Setup(hasher => hasher.Hash("raw-reset-token")).Returns("new-token-hash");

        return new CreatePasswordResetTokenCommandHandler(
            userRepository.Object,
            randomTokenGenerator.Object,
            tokenHasher.Object,
            new FixedAuthSettingsProvider(),
            new FixedDateTimeProvider(utcNow ?? DateTime.UtcNow),
            unitOfWork.Object,
            outboxRepository.Object,
            new FixedTokenProtector());
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        // Burada testlerin kullanacağı sabit UTC zamanı saklıyorum.
        public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FixedAuthSettingsProvider : IAuthSettingsProvider
    {
        // Burada test için varsayılan auth ayarlarını döndürüyorum.
        public AuthSettings GetSettings() => new();
    }

    private sealed class FixedTokenProtector : IPasswordResetTokenProtector
    {
        // Burada test tokenını sabit korumalı değere çeviriyorum.
        public string Protect(string token) => "protected-reset-token";

        // Burada testteki korumalı tokenı sabit ham değere çeviriyorum.
        public string Unprotect(string protectedToken) => "raw-reset-token";
    }
}
