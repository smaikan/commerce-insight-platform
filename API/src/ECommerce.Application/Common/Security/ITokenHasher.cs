namespace ECommerce.Application.Common.Security;

public interface ITokenHasher
{
    string Hash(string token);
}
