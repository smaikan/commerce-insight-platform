using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ContactMessageMutationPersistenceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    // Burada tracked aggregate'a sonradan eklenen activity ve reply nesnelerinin INSERT state'iyle kalıcılaştığını doğruluyorum.
    [Fact]
    public async Task Tracked_Contact_Mutations_Should_Insert_New_Activities_And_Reply()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var message = CreateMessage();
        await context.ContactMessages.AddAsync(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var tracked = await context.ContactMessages
            .Include(item => item.Activities)
            .Include(item => item.Replies).ThenInclude(reply => reply.OutboxMessage)
            .SingleAsync(item => item.Id == message.Id);
        tracked.ChangeStatus(ContactMessageStatus.InProgress, 42, UtcNow.AddMinutes(1));
        tracked.Assign(43, 42, UtcNow.AddMinutes(2));
        tracked.AddInternalNote("Persistence state kontrol notu.", 42, UtcNow.AddMinutes(3));
        context.ChangeTracker.DetectChanges();

        var firstMutationEntries = context.ChangeTracker.Entries<ContactMessageActivity>()
            .Where(entry => entry.Entity.Type != ContactMessageActivityType.Submitted)
            .ToList();
        firstMutationEntries.Should().HaveCount(3);
        firstMutationEntries.Should().OnlyContain(entry => entry.State == EntityState.Added);
        await context.SaveChangesAsync();

        var outbox = EmailOutboxMessage.CreateContactMessageReply(tracked.Email, tracked.Id, UtcNow.AddMinutes(4));
        var reply = tracked.QueueReply(
            "Persistence state kontrol yanıtı.",
            42,
            new string('A', 64),
            new string('B', 64),
            outbox,
            UtcNow.AddMinutes(4));
        outbox.LinkContactReply(reply.Id);
        context.EmailOutbox.Add(outbox);
        context.ChangeTracker.DetectChanges();

        context.Entry(reply).State.Should().Be(EntityState.Added);
        context.ChangeTracker.Entries<ContactMessageActivity>()
            .Where(entry => entry.State == EntityState.Added)
            .Should().HaveCount(2);
        await context.SaveChangesAsync();

        (await context.ContactMessageActivities.CountAsync(item => item.ContactMessageId == tracked.Id)).Should().Be(6);
        (await context.ContactMessageReplies.CountAsync(item => item.ContactMessageId == tracked.Id)).Should().Be(1);
    }

    // Burada persistence testinde kullanılacak kullanıcı ve sipariş FK'si olmayan geçerli contact aggregate'ını oluşturuyorum.
    private static ContactMessage CreateMessage() =>
        new(
            "CNT-STATE0123456789ABCD",
            null,
            "Ada Lovelace",
            "ada@example.com",
            null,
            ContactMessageSubject.OrderSupport,
            null,
            null,
            "Siparişim hakkında ayrıntılı destek rica ediyorum.",
            "2026-08-v1",
            UtcNow.AddDays(-1),
            UtcNow);
}
