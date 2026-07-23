using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class UserSecurityToken : BaseEntity
{
    public long UserId { get; private set; }
    public User User { get; private set; } = null!;
    public UserSecurityTokenType Type { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? InvalidatedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private UserSecurityToken()
    {
    }

    public UserSecurityToken(
        long userId,
        UserSecurityTokenType type,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt)
    {
        if (userId <= 0)
        {
            throw new DomainException("User id is required.");
        }

        if (expiresAt <= createdAt)
        {
            throw new DomainException("Security token expiry date must be in the future.");
        }

        UserId = userId;
        Type = type;
        SetTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        ConcurrencyToken = Guid.NewGuid();
    }

    public UserSecurityToken(
        User user,
        UserSecurityTokenType type,
        string tokenHash,
        DateTime expiresAt,
        DateTime utcNow)
        : this(1, type, tokenHash, expiresAt, utcNow)
    {
        User = user ?? throw new DomainException("User cannot be empty.");
        UserId = user.Id;
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
        return !IsUsed() && !InvalidatedAt.HasValue && !IsExpired(utcNow);
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
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Invalidate(DateTime invalidatedAt)
    {
        if (!CanBeUsed(invalidatedAt))
        {
            return;
        }

        if (invalidatedAt < CreatedAt)
        {
            throw new DomainException("Token invalidation date cannot be earlier than creation date.");
        }

        InvalidatedAt = invalidatedAt;
        ConcurrencyToken = Guid.NewGuid();
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
