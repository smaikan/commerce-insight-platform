using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class EmailOutboxLeasePersistenceTests
{
    // Burada ilk worker'ın aldığı mesajın aktif lease süresince ikinci worker'a verilmediğini doğruluyorum.
    [Fact]
    public async Task Claim_Should_Prevent_Another_Worker_From_Receiving_An_Active_Lease()
    {
        var utcNow = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.EmailOutbox.Add(EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new EmailOutboxRepository(firstContext);
        var secondRepository = new EmailOutboxRepository(secondContext);

        var firstClaim = await firstRepository.ClaimPendingAsync(
            "worker-one",
            utcNow,
            utcNow.AddMinutes(5),
            10);
        var secondClaim = await secondRepository.ClaimPendingAsync(
            "worker-two",
            utcNow.AddMinutes(1),
            utcNow.AddMinutes(6),
            10);

        firstClaim.Should().ContainSingle();
        firstClaim[0].ProcessingWorker.Should().Be("worker-one");
        firstClaim[0].ClaimToken.Should().NotBeNull();
        secondClaim.Should().BeEmpty();
    }

    // Burada lease süresi bittiğinde yeni worker'ın mesajı alıp eski worker'ın tamamlamasını engellediğini doğruluyorum.
    [Fact]
    public async Task Expired_Lease_Should_Be_Reclaimed_And_Reject_Stale_Completion()
    {
        var utcNow = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.EmailOutbox.Add(EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new EmailOutboxRepository(firstContext);
        var secondRepository = new EmailOutboxRepository(secondContext);
        var firstClaim = (await firstRepository.ClaimPendingAsync(
            "worker-one",
            utcNow,
            utcNow.AddMinutes(1),
            10)).Single();
        var secondClaim = (await secondRepository.ClaimPendingAsync(
            "worker-two",
            utcNow.AddMinutes(1),
            utcNow.AddMinutes(6),
            10)).Single();

        var staleCompletion = await firstRepository.CompleteClaimAsync(
            firstClaim.Id,
            firstClaim.ClaimToken!.Value,
            "worker-one",
            utcNow.AddMinutes(1).AddSeconds(1));
        var currentCompletion = await secondRepository.CompleteClaimAsync(
            secondClaim.Id,
            secondClaim.ClaimToken!.Value,
            "worker-two",
            utcNow.AddMinutes(1).AddSeconds(1));

        staleCompletion.Should().BeFalse();
        currentCompletion.Should().BeTrue();

        await using var assertionContext = new AppDbContext(options);
        var persisted = await assertionContext.EmailOutbox.SingleAsync();
        persisted.ProcessedAt.Should().Be(utcNow.AddMinutes(1).AddSeconds(1));
        persisted.ProcessingWorker.Should().BeNull();
        persisted.ClaimToken.Should().BeNull();
    }

    // Burada SMTP öncesi lease yenilemesinin mesajı ilk lease süresi geçse bile başka worker'ın claim etmesini engellediğini doğruluyorum.
    [Fact]
    public async Task Renewed_Lease_Should_Keep_The_Message_Exclusive_To_The_Claiming_Worker()
    {
        var utcNow = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.EmailOutbox.Add(EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new EmailOutboxRepository(firstContext);
        var secondRepository = new EmailOutboxRepository(secondContext);
        var firstClaim = (await firstRepository.ClaimPendingAsync(
            "worker-one",
            utcNow,
            utcNow.AddMinutes(1),
            1)).Single();

        var renewed = await firstRepository.RenewClaimAsync(
            firstClaim.Id,
            firstClaim.ClaimToken!.Value,
            "worker-one",
            utcNow.AddSeconds(30),
            utcNow.AddMinutes(5));
        var secondClaim = await secondRepository.ClaimPendingAsync(
            "worker-two",
            utcNow.AddMinutes(2),
            utcNow.AddMinutes(7),
            1);

        renewed.Should().BeTrue();
        secondClaim.Should().BeEmpty();
    }

    // Burada başka worker'ın aktif lease altındaki mesajı başarısız olarak işaretleyemediğini doğruluyorum.
    // Burada lease'i olmayan süresi geçmiş parola sıfırlama mesajının worker tarafından terminal dead-letter durumuna alındığını ve yeniden claim edilmediğini doğruluyorum.
    [Fact]
    public async Task Expire_Pending_Should_Terminally_Dead_Letter_An_Expired_Message()
    {
        var utcNow = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.EmailOutbox.Add(EmailOutboxMessage.CreatePasswordReset(
                "user@example.com",
                "protected-token",
                utcNow.AddMinutes(-1),
                utcNow.AddMinutes(-31)));
            await seedContext.SaveChangesAsync();
        }

        await using var workerContext = new AppDbContext(options);
        var repository = new EmailOutboxRepository(workerContext);

        var expiredCount = await repository.ExpirePendingAsync(utcNow, 10);
        var claimedMessages = await repository.ClaimPendingAsync(
            "worker-one",
            utcNow,
            utcNow.AddMinutes(5),
            10);

        expiredCount.Should().Be(1);
        claimedMessages.Should().BeEmpty();

        await using var assertionContext = new AppDbContext(options);
        var persisted = await assertionContext.EmailOutbox.SingleAsync();
        persisted.ProcessedAt.Should().BeNull();
        persisted.DeadLetteredAt.Should().Be(utcNow);
        persisted.LastError.Should().Be("Email delivery was skipped because the message expired.");
        persisted.ClaimToken.Should().BeNull();
        persisted.ProcessingWorker.Should().BeNull();
        persisted.LeaseExpiresAt.Should().BeNull();
    }

    // Burada başka worker'ın aktif lease altındaki mesajı başarısız olarak işaretleyemediğini doğruluyorum.
    [Fact]
    public async Task Active_Claim_Should_Reject_Failure_From_Another_Worker()
    {
        var utcNow = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.EmailOutbox.Add(EmailOutboxMessage.CreateWelcome("user@example.com", "User Test", utcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var ownerContext = new AppDbContext(options);
        await using var otherContext = new AppDbContext(options);
        var ownerRepository = new EmailOutboxRepository(ownerContext);
        var otherRepository = new EmailOutboxRepository(otherContext);
        var claim = (await ownerRepository.ClaimPendingAsync(
            "worker-one",
            utcNow,
            utcNow.AddMinutes(5),
            10)).Single();

        var markedAsFailed = await otherRepository.FailClaimAsync(
            claim.Id,
            claim.ClaimToken!.Value,
            "worker-two",
            utcNow.AddMinutes(1),
            "should not be persisted");

        markedAsFailed.Should().BeFalse();

        await using var assertionContext = new AppDbContext(options);
        var persisted = await assertionContext.EmailOutbox.SingleAsync();
        persisted.AttemptCount.Should().Be(0);
        persisted.ProcessingWorker.Should().Be("worker-one");
    }
}
