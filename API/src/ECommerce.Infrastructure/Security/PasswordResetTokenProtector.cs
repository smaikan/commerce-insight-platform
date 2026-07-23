using ECommerce.Application.Common.Security;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerce.Infrastructure.Security;

public sealed class PasswordResetTokenProtector : IPasswordResetTokenProtector
{
    private readonly IDataProtector _protector;

    public PasswordResetTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ECommerce.PasswordResetEmailOutbox.v1");
    }

    public string Protect(string token) => _protector.Protect(token);
    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);
}
