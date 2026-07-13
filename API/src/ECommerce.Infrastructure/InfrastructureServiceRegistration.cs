using ECommerce.Application.Common.Security;
using ECommerce.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthSettingsProvider, AuthSettingsProvider>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRandomTokenGenerator, RandomTokenGenerator>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
