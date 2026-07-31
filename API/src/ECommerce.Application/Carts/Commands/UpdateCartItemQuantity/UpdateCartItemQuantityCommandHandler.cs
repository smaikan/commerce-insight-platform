using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityCommandHandler
    : IRequestHandler<UpdateCartItemQuantityCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartOwnerResolver _ownerResolver;
    private readonly ICartMetricsRecorder _metricsRecorder;
    private readonly IUnitOfWork _unitOfWork;

    // Burada sepet adedi güncellemesi için owner, metrik ve kayıt bağımlılıklarını hazırlıyorum.
    public UpdateCartItemQuantityCommandHandler(
        ICartRepository cartRepository,
        ICartOwnerResolver ownerResolver,
        ICartMetricsRecorder metricsRecorder,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _ownerResolver = ownerResolver;
        _metricsRecorder = metricsRecorder;
        _unitOfWork = unitOfWork;
    }

    // Burada satır sahipliği, güncel ürün durumu, stok ve concurrency kurallarını birlikte uyguluyorum.
    public async Task<CartDto> Handle(
        UpdateCartItemQuantityCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        var cart = await _cartRepository.GetByOwnerForUpdateAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        CartApplicationRules.EnsureExpectedConcurrencyToken(
            cart,
            request.ExpectedConcurrencyToken);
        var item = CartApplicationRules.GetOwnedItem(cart, request.CartItemId);
        var product = item.Product
            ?? throw new NotFoundException("Product was not found.");
        var variant = item.ProductVariant
            ?? throw new NotFoundException("Product variant was not found.");

        CartApplicationRules.EnsurePurchasable(product, variant);
        CartApplicationRules.EnsureStockForQuantity(variant, request.Quantity);

        var addedQuantity = request.Quantity - item.Quantity;
        cart.UpdateItem(item.Id, request.Quantity, variant.Price);
        if (addedQuantity > 0)
        {
            await _metricsRecorder.RecordAddedQuantityAsync(
                product,
                variant,
                addedQuantity,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var savedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken);
        return (savedCart ?? cart).ToDto();
    }
}
