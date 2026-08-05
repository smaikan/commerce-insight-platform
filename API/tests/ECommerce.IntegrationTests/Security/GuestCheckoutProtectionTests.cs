using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using ECommerce.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests.Security;

public sealed class GuestCheckoutProtectionTests
{
    // Burada Redis olmadığında fallback'in checkout için Turnstile zorunlu kıldığını doğruluyorum.
    [Fact]
    public async Task Redis_Fallback_Should_Require_Turnstile()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));

        var action = () => service.EvaluateCheckoutAsync(
            new GuestCheckoutProtectionRequest("192.0.2.1", "cart-a", "guest@example.com", null));

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(428);
        exception.Which.ErrorCode.Should().Be("guest_checkout_challenge_required");
    }

    // Burada fallback sırasında geçerli Turnstile tokenının checkout denemesine izin verdiğini doğruluyorum.
    [Fact]
    public async Task Redis_Fallback_Should_Accept_Valid_Turnstile()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));

        var action = () => service.EvaluateCheckoutAsync(
            new GuestCheckoutProtectionRequest("192.0.2.2", "cart-b", "guest@example.com", "valid-token"));

        await action.Should().NotThrowAsync();
    }

    // Burada IP başına beş denemeden sonra guest-only 429 kodunun üretildiğini doğruluyorum.
    [Fact]
    public async Task Guest_Checkout_Should_Enforce_Ip_Limit()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
                "192.0.2.3", $"cart-{attempt}", $"guest-{attempt}@example.com", "valid-token"));
        }

        var action = () => service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
            "192.0.2.3", "cart-last", "last@example.com", "valid-token"));

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(429);
        exception.Which.ErrorCode.Should().Be("guest_checkout_rate_limited");
    }

    // Burada sipariş/e-posta eşleşmese bile public magic-link ucunun IP limitinden kaçamadığını doğruluyorum.
    [Fact]
    public async Task Unknown_Magic_Link_Requests_Should_Still_Enforce_Ip_Limit()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await service.EvaluateMagicLinkRequestAsync(null, "192.0.2.44");
        }

        var action = () => service.EvaluateMagicLinkRequestAsync(null, "192.0.2.44");

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(429);
        exception.Which.ErrorCode.Should().Be("guest_checkout_rate_limited");
    }

    // Burada farklı IP ve e-postalar kullanılsa bile aynı guest cart session'ın saatlik limiti aşamadığını doğruluyorum.
    [Fact]
    public async Task Guest_Checkout_Should_Enforce_Session_Limit_Independently()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
                $"192.0.2.{60 + attempt}", "shared-cart", $"guest-{attempt}@example.com", "valid-token"));
        }

        var action = () => service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
            "192.0.2.90", "shared-cart", "last@example.com", "valid-token"));

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.ErrorCode.Should().Be("guest_checkout_rate_limited");
    }

    // Burada session ve IP değiştirilse bile aynı normalize e-postanın saatlik limiti aşamadığını doğruluyorum.
    [Fact]
    public async Task Guest_Checkout_Should_Enforce_Email_Limit_Independently()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
                $"198.51.100.{60 + attempt}", $"cart-{attempt}", "same@example.com", "valid-token"));
        }

        var action = () => service.EvaluateCheckoutAsync(new GuestCheckoutProtectionRequest(
            "198.51.100.90", "cart-last", "same@example.com", "valid-token"));

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.ErrorCode.Should().Be("guest_checkout_rate_limited");
    }

    // Burada Redis sayacının artış ve süre verme işlemini tek Lua komutunda yaptığı için kesintide süresiz sayaç bırakmadığını doğruluyorum.
    [Fact]
    public async Task Redis_Counter_Should_Increment_And_Set_Expiry_Atomically()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var database = new Mock<IDatabase>();
        database.Setup(item => item.ScriptEvaluateAsync(
                It.Is<string>(script => script.Contains("INCR", StringComparison.Ordinal) && script.Contains("PEXPIRE", StringComparison.Ordinal)),
                It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), CommandFlags.None))
            .ReturnsAsync(RedisResult.Create((RedisValue)1, ResultType.Integer));
        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(item => item.IsConnected).Returns(true);
        redis.Setup(item => item.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var service = new GuestCheckoutProtectionService(
            cache, new GuestTokenService(), new FixedTurnstileVerifier(TurnstileVerificationResult.Valid), redis.Object);

        await service.EvaluateCheckoutAsync(
            new GuestCheckoutProtectionRequest("203.0.113.1", "cart-atomic", "guest@example.com", null));

        database.Verify(item => item.ScriptEvaluateAsync(
            It.Is<string>(script => script.Contains("PEXPIRE", StringComparison.Ordinal)),
            It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), CommandFlags.None), Times.Exactly(4));
        database.Verify(item => item.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    private sealed class FixedTurnstileVerifier : ITurnstileVerifier
    {
        private readonly TurnstileVerificationResult _result;

        // Burada testte kullanılacak sabit Turnstile sonucunu hazırlıyorum.
        public FixedTurnstileVerifier(TurnstileVerificationResult result)
        {
            _result = result;
        }

        // Burada ağ çağrısı yapmadan sabit Turnstile sonucunu döndürüyorum.
        public Task<TurnstileVerificationResult> VerifyAsync(
            string token,
            string ipAddress,
            CancellationToken cancellationToken = default) => Task.FromResult(_result);
    }
}
