using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class EmailOutboxMessageTests
{
    // Burada hoş geldin mesajının token gerektirmeden doğru türde oluşturulduğunu doğruluyorum.
    [Fact]
    public void Welcome_Message_Should_Be_Ready_For_Queue()
    {
        var utcNow = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

        var message = EmailOutboxMessage.CreateWelcome(" USER@example.com ", " User Test ", utcNow);

        message.Type.Should().Be(EmailOutboxMessageType.Welcome);
        message.Email.Should().Be("user@example.com");
        message.RecipientName.Should().Be("User Test");
        message.ProtectedToken.Should().BeNull();
        message.ExpiresAt.Should().BeNull();
        message.NextAttemptAt.Should().Be(utcNow);
    }

    // Burada parola sıfırlama mesajının korumalı token ve son kullanma zamanı taşıdığını doğruluyorum.
    [Fact]
    public void Password_Reset_Message_Should_Keep_Protected_Token()
    {
        var utcNow = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

        var message = EmailOutboxMessage.CreatePasswordReset(
            "user@example.com",
            "protected-token",
            utcNow.AddMinutes(30),
            utcNow);

        message.Type.Should().Be(EmailOutboxMessageType.PasswordReset);
        message.ProtectedToken.Should().Be("protected-token");
        message.ExpiresAt.Should().Be(utcNow.AddMinutes(30));
        message.IsExpired(utcNow.AddMinutes(29)).Should().BeFalse();
        message.IsExpired(utcNow.AddMinutes(30)).Should().BeTrue();
    }

    // Burada başarısız gönderimlerin artan bekleme süresiyle yeniden planlandığını doğruluyorum.
    [Fact]
    public void Failed_Message_Should_Use_Exponential_Backoff()
    {
        var utcNow = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
        var message = EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow);

        message.RecordFailure(utcNow, "SMTP unavailable");

        message.AttemptCount.Should().Be(1);
        message.NextAttemptAt.Should().Be(utcNow.AddMinutes(2));
        message.LastError.Should().Be("SMTP unavailable");
    }
}
