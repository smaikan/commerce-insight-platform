using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenGenerator(
        IConfiguration configuration,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public AccessTokenResult GenerateAccessToken(User user, Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Session id is required for access token generation.");
        }

        var secretKey = _configuration["Jwt:SecretKey"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("JWT secret key must be configured and at least 32 bytes long.");
        }

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var expiresAt = utcNow.AddMinutes(_authSettingsProvider.GetSettings().AccessTokenMinutes);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var publicUserId = PublicIdCodec.EncodeUserId(user.Id);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, publicUserId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, publicUserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(AuthClaimTypes.SecurityVersion, user.SecurityVersion.ToString()),
            new Claim(AuthClaimTypes.SessionId, sessionId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            utcNow,
            expiresAt,
            credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
