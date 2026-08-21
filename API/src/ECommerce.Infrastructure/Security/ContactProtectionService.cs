using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Security;

public sealed class ContactProtectionService : IContactProtectionService
{
    private const string IncrementWithExpiryScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
        return count
        """;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IGuestTokenService _tokens;
    private readonly ITurnstileVerifier _turnstile;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    // Burada contact korumasının Redis, hash, Turnstile ve ortam bağımlılıklarını hazırlıyorum.
    public ContactProtectionService(
        IGuestTokenService tokens,
        ITurnstileVerifier turnstile,
        IConfiguration configuration,
        IHostEnvironment environment,
        IConnectionMultiplexer? redis = null)
    {
        _tokens = tokens;
        _turnstile = turnstile;
        _configuration = configuration;
        _environment = environment;
        _redis = redis;
    }

    // Burada normalize e-posta ve yalnız güvenilir zincirden gelen IP limitini fail-closed challenge ile uyguluyorum.
    public async Task EvaluateAsync(ContactProtectionRequest request, CancellationToken cancellationToken = default)
    {
        await VerifyChallengeAsync(request, cancellationToken);

        if (_redis is null || !_redis.IsConnected)
        {
            throw ProtectionUnavailable();
        }

        try
        {
            var database = _redis.GetDatabase();
            var emailHash = _tokens.Hash(request.NormalizedEmail);
            var emailCount = await IncrementAsync(database, $"contact:email:{emailHash}", TimeSpan.FromHours(1));
            if (emailCount > 5)
            {
                throw new ApiContractException(429, "contact_submission_rate_limited", "Contact submission rate limited", "İletişim formu gönderim limiti aşıldı.", 3600);
            }

            if (!string.IsNullOrWhiteSpace(request.ClientIpAddress))
            {
                var ipHash = _tokens.Hash(request.ClientIpAddress);
                var ipCount = await IncrementAsync(database, $"contact:ip:{ipHash}", TimeSpan.FromMinutes(15));
                if (ipCount > 10)
                {
                    throw new ApiContractException(429, "contact_submission_rate_limited", "Contact submission rate limited", "İletişim formu gönderim limiti aşıldı.", 900);
                }
            }
        }
        catch (RedisException)
        {
            throw ProtectionUnavailable();
        }
    }

    // Burada challenge'ı kota tüketiminden önce doğrulayarak geçersiz tokenlarla e-posta kotasının zehirlenmesini önlüyorum.
    private async Task VerifyChallengeAsync(ContactProtectionRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsProduction() && string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            throw new ApiContractException(428, "contact_challenge_required", "Contact challenge required", "Devam etmek için güvenlik doğrulamasını tamamlayın.");
        }

        var hostname = _configuration["ContactProtection:Turnstile:Hostname"];
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw ProtectionUnavailable();
        }

        var verification = await _turnstile.VerifyAsync(
            request.TurnstileToken,
            request.ClientIpAddress,
            "contact_form",
            hostname,
            cancellationToken);
        if (verification == TurnstileVerificationResult.Unavailable)
        {
            throw ProtectionUnavailable();
        }

        if (verification != TurnstileVerificationResult.Valid)
        {
            throw new ApiContractException(428, "contact_challenge_required", "Contact challenge required", "Güvenlik doğrulaması geçersiz veya süresi dolmuş.");
        }
    }

    // Burada Redis sayacını atomik artırıp pencere süresini ilk yazımda bağlıyorum.
    private static async Task<long> IncrementAsync(IDatabase database, string key, TimeSpan lifetime)
    {
        var result = await database.ScriptEvaluateAsync(
            IncrementWithExpiryScript,
            [(RedisKey)key],
            [(RedisValue)(long)lifetime.TotalMilliseconds]);
        return (long)result;
    }

    // Burada koruma altyapısı kesintisini güvenli ve kararlı 503 sözleşmesine çeviriyorum.
    private static ApiContractException ProtectionUnavailable() =>
        new(503, "contact_protection_unavailable", "Contact protection unavailable", "İletişim formu koruması geçici olarak kullanılamıyor.");
}
