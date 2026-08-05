using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Security;

public sealed class GuestCheckoutProtectionService : IGuestCheckoutProtectionService
{
    private const string IncrementWithExpiryScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return count
        """;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly IGuestTokenService _tokens;
    private readonly ITurnstileVerifier _turnstile;
    private readonly object _fallbackLock = new();

    // Burada Redis birincil, process içi sayaç fallback ve Turnstile doğrulama bağımlılıklarını hazırlıyorum.
    public GuestCheckoutProtectionService(
        IMemoryCache memoryCache,
        IGuestTokenService tokens,
        ITurnstileVerifier turnstile,
        IConnectionMultiplexer? redis = null)
    {
        _memoryCache = memoryCache;
        _tokens = tokens;
        _turnstile = turnstile;
        _redis = redis;
    }

    // Burada yalnız guest checkout için deneme, IP, kimlik ve challenge kurallarını uyguluyorum.
    public async Task EvaluateCheckoutAsync(
        GuestCheckoutProtectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var ipHash = _tokens.Hash(request.IpAddress);
        var sessionHash = _tokens.Hash(request.CartSessionId);
        var emailHash = _tokens.Hash(request.NormalizedEmail);
        bool fallback;
        long attempts;
        long ipCount;
        long sessionCount;
        long emailCount;
        try
        {
            if (_redis is null || !_redis.IsConnected)
            {
                throw new RedisConnectionException(ConnectionFailureType.UnableToResolvePhysicalConnection, "Redis is unavailable.");
            }

            var database = _redis.GetDatabase();
            attempts = await IncrementAsync(database, $"guest:checkout:attempt:{ipHash}:{sessionHash}:{emailHash}", TimeSpan.FromMinutes(10));
            ipCount = await IncrementAsync(database, $"guest:checkout:ip:{ipHash}", TimeSpan.FromMinutes(15));
            sessionCount = await IncrementAsync(database, $"guest:checkout:session:{sessionHash}", TimeSpan.FromHours(1));
            emailCount = await IncrementAsync(database, $"guest:checkout:email:{emailHash}", TimeSpan.FromHours(1));
            fallback = false;
        }
        catch (RedisException)
        {
            attempts = IncrementFallback($"attempt:{ipHash}:{sessionHash}:{emailHash}", TimeSpan.FromMinutes(10));
            ipCount = IncrementFallback($"ip:{ipHash}", TimeSpan.FromMinutes(15));
            sessionCount = IncrementFallback($"session:{sessionHash}", TimeSpan.FromHours(1));
            emailCount = IncrementFallback($"email:{emailHash}", TimeSpan.FromHours(1));
            fallback = true;
        }

        if (ipCount > 5 || sessionCount > 5 || emailCount > 5)
        {
            throw new ApiContractException(429, "guest_checkout_rate_limited", "Guest checkout rate limited", "Guest checkout deneme limiti aşıldı. Daha sonra tekrar deneyin.");
        }

        var challengeRequired = fallback || attempts >= 3;
        if (!challengeRequired && string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            throw new ApiContractException(428, "guest_checkout_challenge_required", "Guest checkout challenge required", "Devam etmek için güvenlik doğrulamasını tamamlayın.");
        }

        var verification = await _turnstile.VerifyAsync(request.TurnstileToken, request.IpAddress, cancellationToken);
        if (verification == TurnstileVerificationResult.Unavailable)
        {
            throw new ApiContractException(503, "guest_checkout_protection_unavailable", "Guest checkout protection unavailable", "Guest checkout koruması geçici olarak kullanılamıyor.");
        }

        if (verification != TurnstileVerificationResult.Valid)
        {
            throw new ApiContractException(428, "guest_checkout_challenge_required", "Guest checkout challenge required", "Güvenlik doğrulaması geçersiz veya süresi dolmuş.");
        }
    }

    // Burada magic-link isteğini sipariş başına üç ve IP başına on istekle sınırlandırıyorum.
    public async Task EvaluateMagicLinkRequestAsync(Guid? orderId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var ipHash = _tokens.Hash(ipAddress);
        long orderCount;
        long ipCount;
        try
        {
            if (_redis is null || !_redis.IsConnected)
            {
                throw new RedisConnectionException(ConnectionFailureType.UnableToResolvePhysicalConnection, "Redis is unavailable.");
            }

            var database = _redis.GetDatabase();
            ipCount = await IncrementAsync(database, $"guest:magic:ip:{ipHash}", TimeSpan.FromHours(1));
            orderCount = orderId.HasValue
                ? await IncrementAsync(database, $"guest:magic:order:{orderId.Value:N}", TimeSpan.FromHours(1))
                : 0;
        }
        catch (RedisException)
        {
            ipCount = IncrementFallback($"magic-ip:{ipHash}", TimeSpan.FromHours(1));
            orderCount = orderId.HasValue
                ? IncrementFallback($"magic-order:{orderId.Value:N}", TimeSpan.FromHours(1))
                : 0;
        }

        if (orderCount > 3 || ipCount > 10)
        {
            throw new ApiContractException(429, "guest_checkout_rate_limited", "Guest access rate limited", "Erişim bağlantısı istek limiti aşıldı.");
        }
    }

    // Burada Redis sayacını atomik artırıp ilk artışta pencere süresini bağlıyorum.
    private static async Task<long> IncrementAsync(IDatabase database, string key, TimeSpan lifetime)
    {
        var result = await database.ScriptEvaluateAsync(
            IncrementWithExpiryScript,
            [(RedisKey)key],
            [(RedisValue)(long)lifetime.TotalMilliseconds]);
        return (long)result;
    }

    // Burada Redis kesintisinde yalnız bu instance için kısa ömürlü güvenli fallback sayacı artırıyorum.
    private long IncrementFallback(string key, TimeSpan lifetime)
    {
        lock (_fallbackLock)
        {
            var count = _memoryCache.TryGetValue<long>(key, out var existing) ? existing + 1 : 1;
            _memoryCache.Set(key, count, lifetime);
            return count;
        }
    }
}
