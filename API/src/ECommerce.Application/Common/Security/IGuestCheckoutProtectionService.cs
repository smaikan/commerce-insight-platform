namespace ECommerce.Application.Common.Security;

public interface IGuestCheckoutProtectionService
{
    // Burada guest checkout'a özel Redis, fallback ve Turnstile kontrollerini çalıştırma sözleşmesini tanımlıyorum.
    Task EvaluateCheckoutAsync(GuestCheckoutProtectionRequest request, CancellationToken cancellationToken = default);

    // Burada magic-link isteklerini sipariş ve IP pencerelerinde sınırlama sözleşmesini tanımlıyorum.
    Task EvaluateMagicLinkRequestAsync(Guid? orderId, string ipAddress, CancellationToken cancellationToken = default);
}

// Burada guest checkout korunması için hash'lenebilir kimlikleri ve opsiyonel challenge tokenını taşıyorum.
public sealed record GuestCheckoutProtectionRequest(
    string IpAddress,
    string CartSessionId,
    string NormalizedEmail,
    string? TurnstileToken);

public interface ITurnstileVerifier
{
    // Burada Cloudflare tokenının sunucu tarafında tek kullanımlık doğrulanması sözleşmesini tanımlıyorum.
    Task<TurnstileVerificationResult> VerifyAsync(string token, string ipAddress, CancellationToken cancellationToken = default);
}

public enum TurnstileVerificationResult
{
    Valid = 1,
    Invalid = 2,
    Unavailable = 3
}
