using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Burada tarayıcıya verilen ham değeri saklamadan guest sipariş oturumunu temsil ediyorum.
public sealed class GuestOrderSession : BaseEntity
{
    public const int Sha256HexLength = 64;

    public string TokenHash { get; private set; } = null!;
    public string CsrfTokenHash { get; private set; } = null!;
    public string? VerifiedEmailHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    // Burada EF Core'un guest oturumunu oluşturabilmesi için boş kurucuyu tutuyorum.
    private GuestOrderSession()
    {
    }

    // Burada hash'lenmiş erişim ve CSRF değerleriyle süreli guest oturumu oluşturuyorum.
    public GuestOrderSession(string tokenHash, string csrfTokenHash, DateTime utcNow, DateTime expiresAt)
    {
        EnsureUtcRange(utcNow, expiresAt);
        TokenHash = NormalizeHash(tokenHash, "Session token hash");
        CsrfTokenHash = NormalizeHash(csrfTokenHash, "CSRF token hash");
        LastUsedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    // Burada geçerli guest oturumunun kullanım zamanını yeniliyorum.
    public void Touch(DateTime utcNow)
    {
        EnsureActive(utcNow);
        LastUsedAt = utcNow;
    }

    // Burada tek kullanımlık magic link ile doğrulanmış e-posta kanıtını oturuma bağlıyorum.
    public void VerifyEmail(string emailHash, DateTime utcNow)
    {
        EnsureActive(utcNow);
        VerifiedEmailHash = NormalizeHash(emailHash, "Verified email hash");
        LastUsedAt = utcNow;
    }

    // Burada claim veya güvenlik iptali sonrasında guest oturumunu tekrar kullanılamaz hale getiriyorum.
    public void Revoke(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        RevokedAt ??= utcNow;
    }

    // Burada guest oturumunun belirtilen anda kullanılabilir olup olmadığını sorguluyorum.
    public bool IsActiveAt(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return !RevokedAt.HasValue && ExpiresAt > utcNow;
    }

    // Burada aktif olmayan oturumun güvenlik işlemlerinde kullanılmasını engelliyorum.
    private void EnsureActive(DateTime utcNow)
    {
        if (!IsActiveAt(utcNow))
        {
            throw new DomainException("Guest order session is not active.");
        }
    }

    // Burada oturum başlangıç ve bitiş zamanlarının geçerli UTC aralığında olmasını sağlıyorum.
    private static void EnsureUtcRange(DateTime utcNow, DateTime expiresAt)
    {
        EnsureUtc(utcNow);
        EnsureUtc(expiresAt);
        if (expiresAt <= utcNow)
        {
            throw new DomainException("Guest order session expiry must be later than creation time.");
        }
    }

    // Burada SHA-256 hex değerinin kanonik uzunluk ve karakter kümesini doğruluyorum.
    internal static string NormalizeHash(string value, string fieldName)
    {
        if (value is null || value.Length != Sha256HexLength ||
            value.Any(character => character is not (>= '0' and <= '9' or >= 'A' and <= 'F')))
        {
            throw new DomainException($"{fieldName} is invalid.");
        }

        return value;
    }

    // Burada guest güvenlik zamanlarının UTC olmasını zorunlu tutuyorum.
    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Guest order session time must be UTC.");
        }
    }
}
