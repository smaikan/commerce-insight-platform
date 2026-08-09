using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Security;

namespace ECommerce.Infrastructure.Security;

public sealed class GuestTokenService : IGuestTokenService
{
    private const int TokenByteLength = 32;

    // Burada CSPRNG ile 256 bit token üretip ham ve SHA-256 hash biçimlerini ayırıyorum.
    public GuestSecurityToken CreateToken()
    {
        var rawValue = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenByteLength));
        return new GuestSecurityToken(rawValue, Hash(rawValue));
    }

    // Burada hassas guest değerini SHA-256 büyük harfli hex hash'e dönüştürüyorum.
    public string Hash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value to hash cannot be empty.", nameof(value));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));
    }
}
