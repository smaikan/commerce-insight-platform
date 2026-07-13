namespace ECommerce.Application.Common.Security;

public interface IRandomTokenGenerator
{
    string GenerateToken();
}
