using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Returns.Services;
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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ProductUrlGenerator>();
        services.AddScoped<IProductUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());
        services.AddScoped<IUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());
        services.AddScoped<IProductTagResolver, ProductTagResolver>();
        services.AddScoped<ICartOwnerResolver, CartOwnerResolver>();
        services.AddScoped<ICartMetricsRecorder, CartMetricsRecorder>();
        services.AddScoped<IOrderMetricsRecorder, OrderMetricsRecorder>();
        services.AddScoped<OrderInventoryService>();
        services.AddScoped<OrderCouponService>();
        services.AddScoped<OrderPricingService>();
        services.AddScoped<IOrderNotificationService, OrderNotificationService>();
        services.AddScoped<ReturnInventoryService>();

        return services;
    }
}
