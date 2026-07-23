using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerce.Infrastructure;

public static class InfrastructureServiceRegistration
{
    // Burada güvenlik, zaman ve e-posta altyapı servislerini dependency injection'a kaydediyorum.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthSettingsProvider, AuthSettingsProvider>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRandomTokenGenerator, RandomTokenGenerator>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddDataProtection();
        services.AddSingleton<IPasswordResetTokenProtector, PasswordResetTokenProtector>();

        return services;
    }
}
