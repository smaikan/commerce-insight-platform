using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Models;

public sealed class CartOwner
{
    public long? UserId { get; }
    public string? SessionId { get; }
    public bool IsGuest => UserId is null;

    // Burada doğrulanmış kullanıcı veya misafir sepet sahibini tek modelde saklıyorum.
    private CartOwner(long? userId, string? sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }

    // Burada kayıtlı kullanıcı için güvenli sepet sahibi modeli oluşturuyorum.
    public static CartOwner ForUser(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        return new CartOwner(userId, null);
    }

    // Burada normalize edilmiş misafir oturumu için güvenli sepet sahibi modeli oluşturuyorum.
    public static CartOwner ForGuest(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Guest session is required.", nameof(sessionId));
        }

        var normalizedSessionId = sessionId.Trim();
        if (normalizedSessionId.Length > Cart.MaximumSessionIdLength)
        {
            throw new ArgumentException(
                $"Guest session cannot exceed {Cart.MaximumSessionIdLength} characters.",
                nameof(sessionId));
        }

        return new CartOwner(null, normalizedSessionId);
    }
}
