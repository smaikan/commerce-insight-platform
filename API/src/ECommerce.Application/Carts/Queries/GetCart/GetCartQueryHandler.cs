using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Carts.Queries.GetCart;

public sealed class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartOwnerResolver _ownerResolver;

    // Burada sepet sorgusu için owner çözümleyicisini ve repository'yi hazırlıyorum.
    public GetCartQueryHandler(
        ICartRepository cartRepository,
        ICartOwnerResolver ownerResolver)
    {
        _cartRepository = cartRepository;
        _ownerResolver = ownerResolver;
    }

    // Burada güvenli owner üzerinden sepeti getiriyor, kayıt yoksa boş görünüm döndürüyorum.
    public async Task<CartDto> Handle(
        GetCartQuery request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        var cart = await _cartRepository.GetByOwnerAsync(owner, cancellationToken);
        return cart?.ToDto() ?? CartDto.Empty();
    }
}
