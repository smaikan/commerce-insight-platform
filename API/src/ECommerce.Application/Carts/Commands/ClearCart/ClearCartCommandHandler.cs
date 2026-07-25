using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Carts.Commands.ClearCart;

public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartOwnerResolver _ownerResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada güvenli sepet temizleme akışının repository ve owner bağımlılıklarını hazırlıyorum.
    public ClearCartCommandHandler(
        ICartRepository cartRepository,
        ICartOwnerResolver ownerResolver,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _ownerResolver = ownerResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız çözümlenen owner'a ait ve güncel tokenlı sepeti temizliyorum.
    public async Task<CartDto> Handle(
        ClearCartCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        var cart = await _cartRepository.GetByOwnerForUpdateAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        CartApplicationRules.EnsureExpectedConcurrencyToken(
            cart,
            request.ExpectedConcurrencyToken);
        cart.Clear();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken);
        return (savedCart ?? cart).ToDto();
    }
}
