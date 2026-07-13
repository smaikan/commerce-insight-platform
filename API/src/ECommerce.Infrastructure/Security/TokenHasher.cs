using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Security;

namespace ECommerce.Infrastructure.Security;

public sealed class TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Token cannot be empty.");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Base64Url.Encode(hash);
    }
}
