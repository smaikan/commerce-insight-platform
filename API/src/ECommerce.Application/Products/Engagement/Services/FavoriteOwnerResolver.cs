using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Engagement.Services;

public interface IFavoriteOwnerResolver
{
    // Burada mevcut kullanıcı veya misafir oturumundan favori sahibini çözümleme sözleşmesini tanımlıyorum.
    FavoriteOwner Resolve(string? sessionId);
}

public sealed class FavoriteOwnerResolver : IFavoriteOwnerResolver
{
    private readonly ICurrentUserService _currentUser;

    // Burada JWT kullanıcısını favori sahipliğinde önceliklendirmek için servisi hazırlıyorum.
    public FavoriteOwnerResolver(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    // Burada JWT varsa kullanıcıyı, yoksa doğrulanmış guest session'ı favori sahibi yapıyorum.
    public FavoriteOwner Resolve(string? sessionId)
    {
        if (_currentUser.UserId is > 0 and { } userId)
        {
            return FavoriteOwner.ForUser(userId);
        }

        if (string.IsNullOrWhiteSpace(sessionId) ||
            sessionId.Trim().Length > FavoriteProduct.MaximumSessionIdLength)
        {
            throw new UnauthorizedException("A valid guest session is required.");
        }

        return FavoriteOwner.ForGuest(sessionId);
    }
}
