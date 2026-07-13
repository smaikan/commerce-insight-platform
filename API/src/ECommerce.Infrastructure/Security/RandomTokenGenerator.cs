using System.Security.Cryptography;
using ECommerce.Application.Common.Security;

namespace ECommerce.Infrastructure.Security;

public sealed class RandomTokenGenerator : IRandomTokenGenerator
{
    public string GenerateToken()
    {
        return Base64Url.Encode(RandomNumberGenerator.GetBytes(64));
    }
}
