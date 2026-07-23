using System.Security.Cryptography;
using ECommerce.Application.Common.Security;

namespace ECommerce.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;
    private const int MaximumAcceptedIterations = 1_000_000;
    private const char Separator = '.';

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password cannot be empty.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            Separator,
            "PBKDF2-SHA256",
            Iterations,
            Base64Url.Encode(salt),
            Base64Url.Encode(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (!TryParseHash(passwordHash, out var iterations, out var salt, out var expectedHash))
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public bool NeedsRehash(string passwordHash)
    {
        return !TryParseHash(passwordHash, out var iterations, out _, out _) || iterations < Iterations;
    }

    private static bool TryParseHash(
        string passwordHash,
        out int iterations,
        out byte[] salt,
        out byte[] expectedHash)
    {
        iterations = 0;
        salt = [];
        expectedHash = [];

        var parts = passwordHash.Split(Separator);

        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out iterations) ||
            iterations <= 0 ||
            iterations > MaximumAcceptedIterations)
        {
            return false;
        }

        try
        {
            salt = DecodeBase64Url(parts[2]);
            expectedHash = DecodeBase64Url(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length == SaltSize && expectedHash.Length == KeySize;
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');

        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        return Convert.FromBase64String(padded);
    }
}
