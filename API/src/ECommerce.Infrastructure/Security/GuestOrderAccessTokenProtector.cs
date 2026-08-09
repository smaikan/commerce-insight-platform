using ECommerce.Application.Common.Security;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerce.Infrastructure.Security;

public sealed class GuestOrderAccessTokenProtector : IGuestOrderAccessTokenProtector
{
    private readonly IDataProtector _protector;

    // Burada guest magic-link değerleri için amaç ayrımlı Data Protection protector oluşturuyorum.
    public GuestOrderAccessTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ECommerce.GuestOrderAccessToken.v1");
    }

    // Burada ham magic-link tokenını outbox'a yazılmadan önce koruyorum.
    public string Protect(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Guest access token cannot be empty.", nameof(rawToken));
        }

        return _protector.Protect(rawToken);
    }

    // Burada outbox çalışanının magic-link URL'si üretmesi için tokenı geri çözüyorum.
    public string Unprotect(string protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
        {
            throw new ArgumentException("Protected guest access token cannot be empty.", nameof(protectedToken));
        }

        return _protector.Unprotect(protectedToken);
    }
}
