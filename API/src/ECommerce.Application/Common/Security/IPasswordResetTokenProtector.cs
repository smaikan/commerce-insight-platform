namespace ECommerce.Application.Common.Security;

public interface IPasswordResetTokenProtector
{
    string Protect(string token);
    string Unprotect(string protectedToken);
}
