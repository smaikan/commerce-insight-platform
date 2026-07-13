using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class UserRefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private UserRefreshToken()
    {
    }

    public UserRefreshToken(Guid userId, string tokenHash, DateTime expiresAt, string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new DomainException("Refresh token expiry date must be in the future.");
        }

        UserId = userId;
        SetTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedByIp = createdByIp?.Trim();
    }

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAt <= utcNow;
    }

    public bool IsRevoked()
    {
        return RevokedAt.HasValue;
    }

    public bool IsActive(DateTime utcNow)
    {
        return !IsRevoked() && !IsExpired(utcNow);
    }

    public void Revoke(DateTime revokedAt, string? revokedByIp = null, string? replacedByTokenHash = null)
    {
        if (IsRevoked())
        {
            throw new DomainException("Refresh token is already revoked.");
        }

        if (revokedAt < CreatedAt)
        {
            throw new DomainException("Revocation date cannot be earlier than creation date.");
        }

        RevokedAt = revokedAt;
        RevokedByIp = revokedByIp?.Trim();
        ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacedByTokenHash)
            ? null
            : replacedByTokenHash.Trim();
    }

    private void SetTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Refresh token hash cannot be empty.");
        }

        TokenHash = tokenHash.Trim();
    }
}
