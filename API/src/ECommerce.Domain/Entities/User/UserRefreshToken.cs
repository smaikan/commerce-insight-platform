using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class UserRefreshToken : BaseEntity
{
    public long UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? DeviceName { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private UserRefreshToken()
    {
    }

    public UserRefreshToken(
        long userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        string? createdByIp = null,
        string? deviceName = null)
    {
        if (userId <= 0)
        {
            throw new DomainException("User id is required.");
        }

        if (expiresAt <= createdAt)
        {
            throw new DomainException("Refresh token expiry date must be in the future.");
        }

        UserId = userId;
        SetTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp?.Trim();
        DeviceName = deviceName?.Trim();
        ConcurrencyToken = Guid.NewGuid();
    }

    public UserRefreshToken(
        User user,
        string tokenHash,
        DateTime expiresAt,
        DateTime utcNow,
        string? createdByIp = null,
        string? deviceName = null)
        : this(1, tokenHash, expiresAt, utcNow, createdByIp, deviceName)
    {
        User = user ?? throw new DomainException("User cannot be empty.");
        UserId = user.Id;
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
        ConcurrencyToken = Guid.NewGuid();
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
