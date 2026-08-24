using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard;
using ECommerce.Application.Contacts;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Accounting;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        {
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        var configuredLowStockThreshold = configuration[
            $"{DashboardOptions.SectionName}:LowStockThreshold"];
        var lowStockThreshold = string.IsNullOrWhiteSpace(configuredLowStockThreshold)
            ? 10
            : int.TryParse(configuredLowStockThreshold, out var parsedLowStockThreshold)
                ? parsedLowStockThreshold
                : throw new InvalidOperationException(
                    "Dashboard:LowStockThreshold must be a whole number.");
        var dashboardOptions = new DashboardOptions { LowStockThreshold = lowStockThreshold };
        if (dashboardOptions.LowStockThreshold <= 0)
        {
            throw new InvalidOperationException("Dashboard:LowStockThreshold must be greater than zero.");
        }

        services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(dashboardOptions));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductListReader, ProductListReader>();
        services.AddScoped<IPublishedProductListReader, PublishedProductListReader>();
        services.AddScoped<IPublishedProductSearchReader, PublishedProductSearchReader>();
        services.AddScoped<IPublishedProductFacetReader, PublishedProductFacetReader>();
        services.AddScoped<IPublishedCollectionShowcaseReader, PublishedCollectionShowcaseReader>();
        services.AddScoped<IPublishedProductTypeShowcaseReader, PublishedProductTypeShowcaseReader>();
        services.AddScoped<IOrderListReader, OrderListReader>();
        services.AddScoped<IAdminDashboardReader, AdminDashboardReader>();
        services.AddScoped<IProductAnalyticsReader, ProductAnalyticsReader>();
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
        services.AddScoped<IOrderCancellationOperationRepository, OrderCancellationOperationRepository>();
        services.AddScoped<IGuestOrderRepository, GuestOrderRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<IStorefrontBannerRepository, StorefrontBannerRepository>();
        services.AddScoped<IStoreSettingsRepository, StoreSettingsRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserTokenCleanupService, UserTokenCleanupService>();
        services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
        services.AddScoped<ContactMessageRepository>();
        services.AddScoped<IContactMessageRepository>(provider => provider.GetRequiredService<ContactMessageRepository>());
        services.AddScoped<IContactEmailPayloadReader>(provider => provider.GetRequiredService<ContactMessageRepository>());
        services.AddAccountingPersistenceServices();

        return services;
    }
}
