using ECommerce.Application.Dashboard;
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
}
