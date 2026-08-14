using ECommerce.Application.Common.Security;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Security;

public sealed class AuthSettingsProvider : IAuthSettingsProvider
{
    private readonly IConfiguration _configuration;

    // Burada kimlik doğrulama ayarlarını uygulama yapılandırmasından okumaya hazırlıyorum.
    public AuthSettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Burada süre ayarlarını güvenli varsayılanlarla tek bir tipli modelde topluyorum.
    public AuthSettings GetSettings()
    {
        return new AuthSettings
        {
            AccessTokenMinutes = GetInt("Auth:AccessTokenMinutes", 15),
            RefreshTokenDays = GetInt("Auth:RefreshTokenDays", 14),
            PasswordResetTokenMinutes = GetInt("Auth:PasswordResetTokenMinutes", 30),
            PasswordResetRequestCooldownSeconds = GetInt("Auth:PasswordResetRequestCooldownSeconds", 120)
        };
    }

    // Burada pozitif tam sayı ayarını okuyup geçersiz değerlerde güvenli varsayılana dönüyorum.
    private int GetInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }
}
