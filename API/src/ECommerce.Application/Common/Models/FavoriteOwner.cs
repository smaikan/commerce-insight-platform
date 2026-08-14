using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Models;

public sealed class FavoriteOwner
{
    public long? UserId { get; }
    public string? SessionId { get; }
    public bool IsGuest => UserId is null;

    // Burada doğrulanmış kullanıcı veya misafir favori sahibini tek modelde saklıyorum.
    private FavoriteOwner(long? userId, string? sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }

    // Burada kayıtlı kullanıcı için favori sahibi modeli oluşturuyorum.
    public static FavoriteOwner ForUser(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        return new FavoriteOwner(userId, null);
    }

    // Burada normalize edilmiş misafir oturumu için favori sahibi modeli oluşturuyorum.
    public static FavoriteOwner ForGuest(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Guest session is required.", nameof(sessionId));
        }

        var normalizedSessionId = sessionId.Trim();
        if (normalizedSessionId.Length > FavoriteProduct.MaximumSessionIdLength)
        {
            throw new ArgumentException(
                $"Guest session cannot exceed {FavoriteProduct.MaximumSessionIdLength} characters.",
                nameof(sessionId));
        }

        return new FavoriteOwner(null, normalizedSessionId);
    }
}
