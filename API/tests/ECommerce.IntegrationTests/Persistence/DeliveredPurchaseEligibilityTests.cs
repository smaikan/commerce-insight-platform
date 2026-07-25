using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class DeliveredPurchaseEligibilityTests
{
    // Burada yalnızca teslim edilmiş siparişin yorum ve puan hakkı verdiğini doğruluyorum.
    [Fact]
    public async Task Repository_Should_Require_A_Delivered_Order_For_Product_Engagement()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User("buyer@example.com", "hash", "Buyer", "User");
        var product = new Product("Product", "product", "PRODUCT-MAIN");
        var variant = new ProductVariant(product, "Standard", "DELIVERED-SKU", 100m, 5);
        context.AddRange(user, product, variant);
        await context.SaveChangesAsync();

        var order = new Order(user.Id, "ORDER-DELIVERED", 100m, 0m, 0m, 0m, 100m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 100m, 1);
        order.EnsureItemsMatchSubTotal();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new ProductEngagementRepository(context);
        (await repository.HasDeliveredPurchaseAsync(product.Id, user.Id)).Should().BeFalse();

        var utcNow = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "delivered_purchase_payment_001");
        order.AddPayment(payment);
        context.Payments.Add(payment);
        payment.MarkAsPaid("fake_delivered_purchase_transaction_001");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));
        await context.SaveChangesAsync();

        (await repository.HasDeliveredPurchaseAsync(product.Id, user.Id)).Should().BeTrue();
    }

    // Burada ilişkisel test için açık SQLite bağlantısı oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada test DbContext ayarlarını SQLite bağlantısına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
