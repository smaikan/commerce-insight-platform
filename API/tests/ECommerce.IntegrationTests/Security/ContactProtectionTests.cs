using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using ECommerce.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests.Security;

public sealed class ContactProtectionTests
{
    // Burada production challenge geçerli olsa bile Redis yokluğunun sessiz bypass yerine 503 ürettiğini doğruluyorum.
    [Fact]
    public async Task Missing_Redis_Should_Fail_Closed()
    {
        var service = CreateService(TurnstileVerificationResult.Valid);

        var action = () => service.EvaluateAsync(CreateRequest());

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(503);
        exception.Which.ErrorCode.Should().Be("contact_protection_unavailable");
    }

    // Burada geçersiz challenge'ın Redis kotasını tüketmeden 428 ile reddedildiğini doğruluyorum.
    [Fact]
    public async Task Invalid_Challenge_Should_Not_Consume_Redis_Quota()
    {
        var database = new Mock<IDatabase>();
        var redis = CreateConnectedRedis(database);
        var service = CreateService(TurnstileVerificationResult.Invalid, redis.Object);

        var action = () => service.EvaluateAsync(CreateRequest());

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(428);
        exception.Which.ErrorCode.Should().Be("contact_challenge_required");
        database.Verify(item => item.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), CommandFlags.None), Times.Never);
    }

    // Burada normalize e-posta Redis sayacının limit aşımında Retry-After taşıyan 429 ürettiğini doğruluyorum.
    [Fact]
    public async Task Email_Hash_Limit_Should_Return_Retry_After()
    {
        var database = new Mock<IDatabase>();
        var count = 0L;
        database.Setup(item => item.ScriptEvaluateAsync(
                It.Is<string>(script => script.Contains("INCR", StringComparison.Ordinal) && script.Contains("PEXPIRE", StringComparison.Ordinal)),
                It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), CommandFlags.None))
            .Returns(() => Task.FromResult(RedisResult.Create((RedisValue)(++count), ResultType.Integer)));
        var redis = CreateConnectedRedis(database);
        var service = CreateService(TurnstileVerificationResult.Valid, redis.Object);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EvaluateAsync(CreateRequest());
        }

        var action = () => service.EvaluateAsync(CreateRequest());
        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.StatusCode.Should().Be(429);
        exception.Which.ErrorCode.Should().Be("contact_submission_rate_limited");
        exception.Which.RetryAfterSeconds.Should().Be(3600);
    }

    // Burada contact protection servisini production action/hostname yapılandırmasıyla hazırlıyorum.
    private static ContactProtectionService CreateService(
        TurnstileVerificationResult result,
        IConnectionMultiplexer? redis = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ContactProtection:Turnstile:Hostname"] = "www.example.com"
            })
            .Build();
        var environment = Mock.Of<IHostEnvironment>(item => item.EnvironmentName == Environments.Production);
        return new ContactProtectionService(
            new GuestTokenService(),
            new FixedTurnstileVerifier(result),
            configuration,
            environment,
            redis);
    }

    // Burada Redis bağlantı ve database mock'unu bağlı durumda hazırlıyorum.
    private static Mock<IConnectionMultiplexer> CreateConnectedRedis(Mock<IDatabase> database)
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(item => item.IsConnected).Returns(true);
        redis.Setup(item => item.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        return redis;
    }

    // Burada güvenlik testinin hassas olmayan sabit contact protection request'ini hazırlıyorum.
    private static ContactProtectionRequest CreateRequest() =>
        new("customer@example.com", null, "valid-turnstile-token");

    private sealed class FixedTurnstileVerifier : ITurnstileVerifier
    {
        private readonly TurnstileVerificationResult _result;

        // Burada test Turnstile sonucunu ağ çağrısı olmadan saklıyorum.
        public FixedTurnstileVerifier(TurnstileVerificationResult result) => _result = result;

        // Burada eski guest doğrulama sözleşmesi için de sabit sonucu döndürüyorum.
        public Task<TurnstileVerificationResult> VerifyAsync(string token, string ipAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        // Burada contact action ve hostname doğrulama sözleşmesi için sabit sonucu döndürüyorum.
        public Task<TurnstileVerificationResult> VerifyAsync(
            string token,
            string? ipAddress,
            string expectedAction,
            string expectedHostname,
            CancellationToken cancellationToken = default) => Task.FromResult(_result);
    }
}
