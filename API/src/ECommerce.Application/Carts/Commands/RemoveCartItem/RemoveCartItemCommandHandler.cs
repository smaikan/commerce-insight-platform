using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Carts.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler
    : IRequestHandler<RemoveCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartOwnerResolver _ownerResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada güvenli sepet satırı silme akışının repository ve owner bağımlılıklarını hazırlıyorum.
    public RemoveCartItemCommandHandler(
        ICartRepository cartRepository,
        ICartOwnerResolver ownerResolver,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _ownerResolver = ownerResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız çözümlenen owner'a ait ve güncel tokenlı sepet satırını kaldırıyorum.
    public async Task<CartDto> Handle(
        RemoveCartItemCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        var cart = await _cartRepository.GetByOwnerForUpdateAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        CartApplicationRules.EnsureExpectedConcurrencyToken(
            cart,
            request.ExpectedConcurrencyToken);
        var item = CartApplicationRules.GetOwnedItem(cart, request.CartItemId);
        cart.RemoveItem(item.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken);
        return (savedCart ?? cart).ToDto();
    }
}
