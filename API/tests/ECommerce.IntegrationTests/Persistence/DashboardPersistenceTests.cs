using ECommerce.Application.Dashboard;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class DashboardPersistenceTests
{
    // Burada dashboard aggregate sorgusunun metin durumlu siparişi doğru saydığını doğruluyorum.
    [Fact]
    public async Task DashboardReader_Should_Return_Empty_Overview()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Orders.Add(new ECommerce.Domain.Entities.Order(
            null,
            "ORD-DASHBOARD-001",
            0m,
            0m,
            0m,
            0m,
            0m));
        await context.SaveChangesAsync();
        var reader = new AdminDashboardReader(
            context,
            Options.Create(new DashboardOptions { LowStockThreshold = 5 }));

        var overview = await reader.GetOverviewAsync();

        overview.TotalOrderCount.Should().Be(1);
        overview.PendingOrderCount.Should().Be(1);
        overview.PaidOrderCount.Should().Be(0);
        overview.PaidRevenue.Should().Be(0m);
        overview.ActiveProductCount.Should().Be(0);
        overview.LowStockVariantCount.Should().Be(0);
    }

    // Burada iş kuyruğunun yalnız Pending, Confirmed, Paid siparişleri ve New iletişim mesajlarını saydığını doğruluyorum.
    [Fact]
    public async Task DashboardReader_Should_Return_Only_Actionable_Work_Queue_Counts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Orders.AddRange(
            CreateOrder("ORD-WORK-001"),
            CreateOrder("ORD-WORK-002"),
            CreateOrder("ORD-WORK-003"),
            CreateOrder("ORD-WORK-004"));
        var createdAtUtc = new DateTime(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc);
        context.ContactMessages.AddRange(
            CreateContactMessage("MSG-WORK-001", createdAtUtc),
            CreateContactMessage("MSG-WORK-002", createdAtUtc.AddMinutes(1)));
        await context.SaveChangesAsync();
        var confirmedOrderNumber = "ORD-WORK-002";
        var paidOrderNumber = "ORD-WORK-003";
        var preparingOrderNumber = "ORD-WORK-004";
        var progressedMessageReference = "MSG-WORK-002";
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Orders] SET [Status] = {OrderStatus.Confirmed.ToString()} WHERE [OrderNumber] = {confirmedOrderNumber}");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Orders] SET [Status] = {OrderStatus.Paid.ToString()} WHERE [OrderNumber] = {paidOrderNumber}");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Orders] SET [Status] = {OrderStatus.Preparing.ToString()} WHERE [OrderNumber] = {preparingOrderNumber}");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ContactMessages] SET [Status] = {ContactMessageStatus.InProgress.ToString()} WHERE [ReferenceNumber] = {progressedMessageReference}");
        var reader = new AdminDashboardReader(
            context,
            Options.Create(new DashboardOptions { LowStockThreshold = 5 }));

        var summary = await reader.GetWorkQueueSummaryAsync();

        summary.OrdersAwaitingProcessingCount.Should().Be(3);
        summary.NewContactMessageCount.Should().Be(1);
        summary.GeneratedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    // Burada iş kuyruğu testi için başlangıç durumundaki siparişi oluşturuyorum.
    private static Order CreateOrder(string orderNumber) =>
        new(null, orderNumber, 0m, 0m, 0m, 0m, 0m);

    // Burada iş kuyruğu testi için başlangıç durumundaki iletişim mesajını oluşturuyorum.
    private static ContactMessage CreateContactMessage(string referenceNumber, DateTime createdAtUtc) =>
        new(
            referenceNumber,
            null,
            "Test Kullanıcı",
            "test@example.com",
            null,
            ContactMessageSubject.Other,
            null,
            null,
            "İş kuyruğu sayacı için yeterince uzun test mesajı.",
            "v1",
            createdAtUtc,
            createdAtUtc);
}
