using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Accounting;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence;

public static class PersistenceServiceRegistration
{
    // Burada SQL Server bağlamını ve persistence servislerini dependency injection'a kaydediyorum.
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductListReader, ProductListReader>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IVariantOptionResolver, VariantOptionResolver>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductEngagementRepository, ProductEngagementRepository>();
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserTokenCleanupService, UserTokenCleanupService>();
        services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
        services.AddAccountingPersistenceServices();

        return services;
    }
}
