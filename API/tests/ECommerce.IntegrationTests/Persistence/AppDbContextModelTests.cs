using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class AppDbContextModelTests
{
    // Burada EF Core modelinin tüm e-ticaret entity'lerini içerdiğini doğruluyorum.
    [Fact]
    public void Model_Should_Include_All_ECommerce_Entities()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var entityTypes = context.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToList();

        entityTypes.Should().Contain(new[]
        {
            typeof(Address),
            typeof(Brand),
            typeof(Cart),
            typeof(CartItem),
            typeof(Collection),
            typeof(Coupon),
            typeof(CouponUsage),
            typeof(FavoriteProduct),
            typeof(InventoryTransaction),
            typeof(Order),
            typeof(OrderItem),
            typeof(Payment),
            typeof(EmailOutboxMessage),
            typeof(Product),
            typeof(ProductBundleItem),
            typeof(ProductCollection),
            typeof(ProductDailyMetric),
            typeof(ProductImage),
            typeof(ProductRating),
            typeof(ProductReview),
            typeof(ProductTag),
            typeof(ProductType),
            typeof(ProductVariant),
            typeof(ProductVariantDailyMetric),
            typeof(Tag),
            typeof(User),
            typeof(UserRefreshToken),
            typeof(UserSecurityToken)
        });
    }
}
