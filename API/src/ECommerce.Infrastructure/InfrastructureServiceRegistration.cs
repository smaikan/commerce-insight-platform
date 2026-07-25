using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Email;
using ECommerce.Infrastructure.Payments;
using ECommerce.Application.Common.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure;

public static class InfrastructureServiceRegistration
{
    // Burada güvenlik, zaman ve e-posta altyapı servislerini dependency injection'a kaydediyorum.
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddSingleton<IAuthSettingsProvider, AuthSettingsProvider>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRandomTokenGenerator, RandomTokenGenerator>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<FakePaymentGateway>();
        services.AddSingleton<IPaymentGateway>(provider => provider.GetRequiredService<FakePaymentGateway>());
        services.AddSingleton<IPaymentGatewayReconciler>(provider => provider.GetRequiredService<FakePaymentGateway>());
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(configuration?["DataProtection:ApplicationName"] ?? "ECommerce.API");
        var keyRingPath = configuration?["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            Directory.CreateDirectory(keyRingPath);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

        services.AddSingleton<IPasswordResetTokenProtector, PasswordResetTokenProtector>();

        return services;
    }
}
