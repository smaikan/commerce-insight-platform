using ECommerce.Application.Common.Security;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Security;

public sealed class AuthSettingsProvider : IAuthSettingsProvider
{
    private readonly IConfiguration _configuration;

    public AuthSettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthSettings GetSettings()
    {
        return new AuthSettings
        {
            AccessTokenMinutes = GetInt("Auth:AccessTokenMinutes", 15),
            RefreshTokenDays = GetInt("Auth:RefreshTokenDays", 14),
            EmailConfirmationTokenHours = GetInt("Auth:EmailConfirmationTokenHours", 24),
            PasswordResetTokenMinutes = GetInt("Auth:PasswordResetTokenMinutes", 30),
            MaxFailedAccessAttempts = GetInt("Auth:MaxFailedAccessAttempts", 5),
            LockoutMinutes = GetInt("Auth:LockoutMinutes", 15)
        };
    }

    private int GetInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }
}
