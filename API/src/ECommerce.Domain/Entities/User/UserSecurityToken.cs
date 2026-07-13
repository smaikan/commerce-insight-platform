using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class UserSecurityToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public UserSecurityTokenType Type { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private UserSecurityToken()
    {
    }

    public UserSecurityToken(Guid userId, UserSecurityTokenType type, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new DomainException("Security token expiry date must be in the future.");
        }

        UserId = userId;
        Type = type;
        SetTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAt <= utcNow;
    }

    public bool IsUsed()
    {
        return UsedAt.HasValue;
    }

    public bool CanBeUsed(DateTime utcNow)
    {
        return !IsUsed() && !IsExpired(utcNow);
    }

    public void MarkAsUsed(DateTime usedAt)
    {
        if (IsUsed())
        {
            throw new DomainException("Security token is already used.");
        }

        if (usedAt < CreatedAt)
        {
            throw new DomainException("Token usage date cannot be earlier than creation date.");
        }

        if (IsExpired(usedAt))
        {
            throw new DomainException("Expired security token cannot be used.");
        }

        UsedAt = usedAt;
    }

    private void SetTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Security token hash cannot be empty.");
        }

        TokenHash = tokenHash.Trim();
    }
}
