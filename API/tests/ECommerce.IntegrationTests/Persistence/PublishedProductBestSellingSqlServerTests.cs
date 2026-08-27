using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Migrations;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class PublishedProductBestSellingSqlServerTests
{
    // Burada çok satan ve popülerlik sıralamalarının SQL Server'da sayfalama öncesi doğru ve kararlı çalıştığını doğruluyorum.
    [SqlServerFact]
    public async Task Reader_Should_Sort_BestSelling_And_Popularity_Before_Pagination()
    {
        var databaseName = $"ECommerceBestSelling_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            await using var context = new AppDbContext(options);
            await context.Database.MigrateAsync();
            var first = CreatePublishedProduct("First tied seller", "first-tied", "FIRST", 5, 1);
            var second = CreatePublishedProduct("Second tied seller", "second-tied", "SECOND", 5, 2);
            var popular = CreatePublishedProduct("Popularity leader", "popularity-leader", "POPULAR", 3, 10);
            context.Products.AddRange(first, second, popular);
            await context.SaveChangesAsync();
            var reader = new PublishedProductListReader(context);

            var bestSellingPageOne = await reader.GetListAsync(new PublishedProductListFilter(
                1,
                2,
                SortBy: PublishedProductSortBy.BestSelling,
                Descending: true));
            var bestSellingPageTwo = await reader.GetListAsync(new PublishedProductListFilter(
                2,
                2,
                SortBy: PublishedProductSortBy.BestSelling,
                Descending: true));
            var popularity = await reader.GetListAsync(new PublishedProductListFilter(
                1,
                3,
                SortBy: PublishedProductSortBy.Popularity,
                Descending: true));

            bestSellingPageOne.Items.Select(item => item.Title)
                .Should().Equal("First tied seller", "Second tied seller");
            bestSellingPageTwo.Items.Select(item => item.Title)
                .Should().Equal("Popularity leader");
            popularity.Items.First().Title.Should().Be("Popularity leader");
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada geçmiş Paid ve kısmi refund kayıtlarından backfill'in iki çalıştırmada aynı net sonucu ürettiğini doğruluyorum.
    [SqlServerFact]
    public async Task Backfill_Should_Be_Rerunnable_For_Paid_And_Partially_Refunded_Items()
    {
        var databaseName = $"ECommerceSalesBackfill_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            await using var context = new AppDbContext(options);
            await context.Database.MigrateAsync();
            var product = CreatePublishedProduct("Backfill product", "backfill-product", "BACKFILL", 1, 0);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            var variant = product.Variants.Single();
            var now = DateTime.UtcNow.AddMinutes(1);
            var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..24], 30m, 0m, 0m, 0m, 30m);
            var orderItem = order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 3);
            order.ChangeStatus(OrderStatus.Confirmed, now);
            var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal);
            order.AddPayment(payment);
            payment.MarkAsPaid($"backfill-paid-{Guid.NewGuid():N}");
            order.ChangeStatus(OrderStatus.Paid, now);
            order.ChangeStatus(OrderStatus.Preparing, now.AddMinutes(1));
            order.ChangeStatus(OrderStatus.Shipped, now.AddMinutes(2));
            order.ChangeStatus(OrderStatus.Delivered, now.AddMinutes(3));
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            var returnRequest = new ReturnRequest(order.Id, null, "RET-BACKFILL", ReturnType.Refund);
            returnRequest.AddItem(orderItem, 1);
            returnRequest.Receive(now.AddMinutes(4));
            returnRequest.Approve(now.AddMinutes(5));
            context.ReturnRequests.Add(returnRequest);
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Products SET NetSalesQuantity = 0; " +
                "UPDATE OrderItems SET PaidSalesQuantity = 0, ReversedSalesQuantity = 0; " +
                "UPDATE ReturnItems SET SalesMetricReversedQuantity = 0;");

            await ApplyBackfillAsync(context);
            await ApplyBackfillAsync(context);
            context.ChangeTracker.Clear();

            var savedProduct = await context.Products.SingleAsync(candidate => candidate.Id == product.Id);
            var savedOrderItem = await context.OrderItems.SingleAsync(candidate => candidate.Id == orderItem.Id);
            var savedReturnItem = await context.ReturnItems.SingleAsync(candidate =>
                candidate.ReturnRequestId == returnRequest.Id);
            savedProduct.NetSalesQuantity.Should().Be(2);
            savedOrderItem.PaidSalesQuantity.Should().Be(3);
            savedOrderItem.ReversedSalesQuantity.Should().Be(1);
            savedReturnItem.SalesMetricReversedQuantity.Should().Be(1);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada satış ve popülerlik değerleri birbirinden farklı etkin storefront ürününü hazırlıyorum.
    private static Product CreatePublishedProduct(
        string title,
        string url,
        string sku,
        int netSalesQuantity,
        int clickCount)
    {
        var product = new Product(title, url, $"{sku}-MAIN", status: ProductStatus.Active);
        product.Variants.Add(new ProductVariant(product, "Standard", $"{sku}-STD", 100m, 5));
        product.IncreaseNetSalesQuantity(netSalesQuantity);
        for (var index = 0; index < clickCount; index++)
        {
            product.IncreaseClickCount();
        }

        return product;
    }

    // Burada migration ile aynı atama tabanlı backfill SQL'ini ürün ve iade intent'i için birlikte çalıştırıyorum.
    private static async Task ApplyBackfillAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(AuthoritativeSalesMetricBackfill.ProductSalesSql);
        await context.Database.ExecuteSqlRawAsync(AuthoritativeSalesMetricBackfill.ReturnIntentSql);
    }

    // Burada yalnız açıkça yapılandırılmış SQL Server test ortamına benzersiz veritabanı seçenekleri üretiyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var connectionString = OperatingSystem.IsWindows() &&
                               (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(password))
            ? $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;"
            : new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = databaseName,
                UserID = "sa",
                Password = password,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            }.ConnectionString;

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }
}
