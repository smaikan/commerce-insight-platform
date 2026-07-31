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
        message.DeduplicationKey.Should().StartWith("welcome:");
        message.ConcurrencyToken.Should().NotBe(Guid.Empty);
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
    // Burada süresi geçen parola sıfırlama mesajının lease'i temizlenerek tekrar claim edilemeyen terminal dead-letter durumuna geçtiğini doğruluyorum.
    [Fact]
    public void Expired_Password_Reset_Message_Should_Be_Terminally_Dead_Lettered()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = utcNow.AddMinutes(1);
        var message = EmailOutboxMessage.CreatePasswordReset(
            "user@example.com",
            "protected-token",
            expiresAt,
            utcNow);

        message.ClaimForProcessing("worker-one", Guid.NewGuid(), utcNow.AddMinutes(10), utcNow);
        message.LeaseExpiresAt.Should().Be(expiresAt);

        message.MarkExpired(expiresAt);

        message.DeadLetteredAt.Should().Be(expiresAt);
        message.LastError.Should().Be("Email delivery was skipped because the message expired.");
        message.ClaimToken.Should().BeNull();
        message.ProcessingWorker.Should().BeNull();
        message.LeaseExpiresAt.Should().BeNull();
        message.IsEligibleForClaim(expiresAt.AddDays(1)).Should().BeFalse();
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

    // Burada sipariş bildiriminin olay kimliğine göre tekrar üretildiğinde aynı anahtarı taşıdığını doğruluyorum.
    [Fact]
    public void Order_Created_Message_Should_Use_A_Deterministic_Deduplication_Key()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var orderId = Guid.NewGuid();

        var firstMessage = EmailOutboxMessage.CreateOrderCreated(
            "user@example.com",
            "User Test",
            orderId,
            "ORD-ABC123",
            120.50m,
            utcNow);
        var secondMessage = EmailOutboxMessage.CreateOrderCreated(
            "user@example.com",
            "User Test",
            orderId,
            "ORD-ABC123",
            120.50m,
            utcNow.AddSeconds(1));

        firstMessage.Type.Should().Be(EmailOutboxMessageType.OrderCreated);
        firstMessage.DeduplicationKey.Should().Be($"order-created:{orderId:N}");
        secondMessage.DeduplicationKey.Should().Be(firstMessage.DeduplicationKey);
        firstMessage.OrderNumber.Should().Be("ORD-ABC123");
        firstMessage.Amount.Should().Be(120.50m);
    }

    // Burada claim edilen mesajın yeniden claim edilemediğini ve hata halinde lease bilgisinin temizlendiğini doğruluyorum.
    [Fact]
    public void Claimed_Message_Should_Release_Its_Lease_When_A_Failure_Is_Recorded()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var message = EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow);
        var claimToken = Guid.NewGuid();

        message.ClaimForProcessing("worker-one", claimToken, utcNow.AddMinutes(5), utcNow);

        message.IsEligibleForClaim(utcNow.AddMinutes(1)).Should().BeFalse();
        message.HasActiveClaim("worker-one", claimToken, utcNow.AddMinutes(1)).Should().BeTrue();

        message.RecordFailure(utcNow.AddMinutes(1), "SMTP unavailable");

        message.ClaimToken.Should().BeNull();
        message.ProcessingWorker.Should().BeNull();
        message.LeaseExpiresAt.Should().BeNull();
        message.NextAttemptAt.Should().Be(utcNow.AddMinutes(3));
    }

    // Burada SMTP öncesi lease yenilemenin claim sahibini koruduğunu ve yeni süreyi kalıcı duruma hazırladığını doğruluyorum.
    [Fact]
    public void Claimed_Message_Should_Renew_An_Active_Lease()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var message = EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow);
        var claimToken = Guid.NewGuid();

        message.ClaimForProcessing("worker-one", claimToken, utcNow.AddMinutes(1), utcNow);
        var renewed = message.RenewClaim(
            "worker-one",
            claimToken,
            utcNow.AddMinutes(6),
            utcNow.AddSeconds(30));

        renewed.Should().BeTrue();
        message.LeaseExpiresAt.Should().Be(utcNow.AddMinutes(6));
        message.HasActiveClaim("worker-one", claimToken, utcNow.AddMinutes(5)).Should().BeTrue();
    }

    // Burada sürekli başarısız SMTP mesajının sınırlı denemeden sonra tekrar claim edilemeyen dead-letter durumuna geçtiğini doğruluyorum.
    [Fact]
    public void Failed_Message_Should_Be_Dead_Lettered_After_Maximum_Attempts()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var message = EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow);

        DateTime deadLetterAttemptTime = default;
        for (var attempt = 1; attempt <= EmailOutboxMessage.MaximumDeliveryAttempts; attempt++)
        {
            var attemptTime = message.NextAttemptAt;
            message.ClaimForProcessing(
                "worker-one",
                Guid.NewGuid(),
                attemptTime.AddMinutes(1),
                attemptTime);
            message.RecordFailure(attemptTime, "SMTP unavailable");
            deadLetterAttemptTime = attemptTime;
        }

        message.AttemptCount.Should().Be(EmailOutboxMessage.MaximumDeliveryAttempts);
        message.DeadLetteredAt.Should().Be(deadLetterAttemptTime);
        message.IsEligibleForClaim(utcNow.AddDays(1)).Should().BeFalse();
    }
}
