using System.IdentityModel.Tokens.Jwt;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ECommerce.IntegrationTests.Security;

public sealed class SecurityServiceTests
{
    [Fact]
    public void PasswordHasher_Should_Verify_Correct_Password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("StrongPassword123!");

        hasher.Verify("StrongPassword123!", hash).Should().BeTrue();
        hasher.Verify("WrongPassword123!", hash).Should().BeFalse();
        hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("PBKDF2-SHA256.210000.invalid-base64!.invalid-base64!")]
    [InlineData("PBKDF2-SHA256.999999999.AA.AA")]
    [InlineData("unsupported")]
    public void PasswordHasher_Should_Reject_Malformed_Hash_Without_Throwing(string hash)
    {
        var hasher = new PasswordHasher();

        var act = () => hasher.Verify("StrongPassword123!", hash);

        act.Should().NotThrow().Which.Should().BeFalse();
    }

    [Fact]
    public void JwtTokenGenerator_Should_Include_SecurityVersion_Claim()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-that-is-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "ECommerce.Tests",
                ["Jwt:Audience"] = "ECommerce.Tests.Client"
            })
            .Build();
        var user = new User("user@example.com", "password-hash", "User", "Test");
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(user, 42L);
        var generator = new JwtTokenGenerator(
            configuration,
            new FixedAuthSettingsProvider(),
            new FixedDateTimeProvider(new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc)));

        var sessionId = Guid.NewGuid();
        var result = generator.GenerateAccessToken(user, sessionId);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Claims.Single(claim => claim.Type == AuthClaimTypes.SecurityVersion)
            .Value.Should().Be(user.SecurityVersion.ToString());
        token.Claims.Single(claim => claim.Type == AuthClaimTypes.SessionId)
            .Value.Should().Be(sessionId.ToString());
        token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub)
            .Value.Should().Be(PublicIdCodec.EncodeUserId(user.Id));
    }

    private sealed class FixedAuthSettingsProvider : IAuthSettingsProvider
    {
        public AuthSettings GetSettings() => new();
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
