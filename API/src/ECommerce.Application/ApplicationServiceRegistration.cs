using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Accounting;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Orders.Commands.ImportOrders;
using ECommerce.Application.Returns.Services;
using ECommerce.Application.GuestOrders;
using ECommerce.Application.StoreSettings;
using ECommerce.Application.GuestSessions.Services;
using ECommerce.Application.Products.Engagement.Services;
using ECommerce.Application.Payments;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class ApplicationServiceRegistration
{
    // Burada Application katmanının MediatR, doğrulama ve ortak servis bağımlılıklarını kaydediyorum.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        services.AddAccountingApplicationServices();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ProductUrlGenerator>();
        services.AddScoped<IProductUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());
        services.AddScoped<IUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());
        services.AddScoped<IProductUrlResolver, ProductUrlResolver>();
        services.AddScoped<IProductTagResolver, ProductTagResolver>();
        services.AddScoped<IProductTypeNameResolver, ProductTypeNameResolver>();
        services.AddScoped<IProductCollectionNameResolver, ProductCollectionNameResolver>();
        services.AddScoped<ICartOwnerResolver, CartOwnerResolver>();
        services.AddScoped<IFavoriteOwnerResolver, FavoriteOwnerResolver>();
        services.AddScoped<IGuestSessionClaimService, GuestSessionClaimService>();
        services.AddScoped<ICartMetricsRecorder, CartMetricsRecorder>();
        services.AddScoped<StoreSettingsService>();
        services.AddScoped<IOrderMetricsRecorder, OrderMetricsRecorder>();
        services.AddScoped<ImportedOrderProcessor>();
        services.AddScoped<OrderInventoryService>();
        services.AddScoped<OrderCouponService>();
        services.AddScoped<OrderPricingService>();
        services.AddScoped<OrderCheckoutOrchestrator>();
        services.AddScoped<GuestOrderAccessService>();
        services.AddScoped<GuestOrderOperationsService>();
        services.AddScoped<CheckoutFormPaymentService>();
        services.AddScoped<IOrderNotificationService, OrderNotificationService>();
        services.AddScoped<ReturnInventoryService>();

        return services;
    }
}
