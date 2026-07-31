using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Context;

public sealed class AppDbContext : DbContext
{
    // Burada EF Core bağlamını dışarıdan verilen veritabanı seçenekleriyle oluşturuyorum.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAddressSnapshot> OrderAddressSnapshots => Set<OrderAddressSnapshot>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBundleItem> ProductBundleItems => Set<ProductBundleItem>();
    public DbSet<ProductCollection> ProductCollections => Set<ProductCollection>();
    public DbSet<ProductDailyMetric> ProductDailyMetrics => Set<ProductDailyMetric>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductRating> ProductRatings => Set<ProductRating>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantDailyMetric> ProductVariantDailyMetrics => Set<ProductVariantDailyMetric>();
    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues => Set<ProductVariantOptionValue>();
    public DbSet<VariantOptionName> VariantOptionNames => Set<VariantOptionName>();
    public DbSet<VariantOptionValue> VariantOptionValues => Set<VariantOptionValue>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<UserSecurityToken> UserSecurityTokens => Set<UserSecurityToken>();

    // Burada entity configuration sınıflarını otomatik olarak modele uyguluyorum.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
