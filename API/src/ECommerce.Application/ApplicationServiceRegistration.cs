using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ProductUrlGenerator>();
        services.AddScoped<IProductUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());
        services.AddScoped<IUrlGenerator>(provider => provider.GetRequiredService<ProductUrlGenerator>());

        return services;
    }
}
