using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts.Services;

public interface ICartOwnerResolver
{
    // Burada mevcut kullanıcı veya misafir oturumundan güvenli sepet sahibini çözümleme sözleşmesini tanımlıyorum.
    CartOwner Resolve(string? sessionId);
}

public sealed class CartOwnerResolver : ICartOwnerResolver
{
    private readonly ICurrentUserService _currentUser;

    // Burada sepet sahibini JWT kullanıcısından çözmek için mevcut kullanıcı servisini hazırlıyorum.
    public CartOwnerResolver(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    // Burada giriş yapmış kullanıcıyı önceliklendirip anonim istekte geçerli misafir oturumunu zorunlu tutuyorum.
    public CartOwner Resolve(string? sessionId)
    {
        if (_currentUser.UserId is > 0 and { } userId)
        {
            return CartOwner.ForUser(userId);
        }

        if (string.IsNullOrWhiteSpace(sessionId) ||
            sessionId.Trim().Length > Cart.MaximumSessionIdLength)
        {
            throw new UnauthorizedException("A valid guest cart session is required.");
        }

        return CartOwner.ForGuest(sessionId);
    }
}
