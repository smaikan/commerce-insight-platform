using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly ICartOwnerResolver _ownerResolver;
    private readonly ICartMetricsRecorder _metricsRecorder;
    private readonly IUnitOfWork _unitOfWork;

    // Burada güvenilir fiyatla sepete ekleme akışının tüm bağımlılıklarını hazırlıyorum.
    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        ICartOwnerResolver ownerResolver,
        ICartMetricsRecorder metricsRecorder,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _ownerResolver = ownerResolver;
        _metricsRecorder = metricsRecorder;
        _unitOfWork = unitOfWork;
    }

    // Burada owner'a ait sepeti bulma veya oluşturma işlemini serializable transaction içinde yürütüyorum.
    public Task<CartDto> Handle(
        AddCartItemCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken =>
                AddItemInTransactionAsync(request, owner, transactionCancellationToken),
            cancellationToken);
    }

    // Burada varyantı güvenilir kaynaktan doğrulayıp sepete ve metriklere aynı işlemde ekliyorum.
    private async Task<CartDto> AddItemInTransactionAsync(
        AddCartItemCommand request,
        CartOwner owner,
        CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(
            request.ProductVariantId,
            cancellationToken)
            ?? throw new NotFoundException("Product variant was not found.");
        var product = await _productRepository.GetByIdForUpdateAsync(
            variant.ProductId,
            cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        CartApplicationRules.EnsurePurchasable(product, variant);

        var cart = await _cartRepository.GetByOwnerForUpdateAsync(owner, cancellationToken);
        if (cart is null)
        {
            if (request.ExpectedConcurrencyToken.HasValue)
            {
                throw new ConcurrencyException(
                    "The cart no longer exists. Refresh the cart and try again.");
            }

            cart = owner.IsGuest
                ? Cart.CreateForGuest(owner.SessionId!)
                : Cart.CreateForUser(owner.UserId!.Value);
            await _cartRepository.AddAsync(cart, cancellationToken);
        }
        else
        {
            if (!request.ExpectedConcurrencyToken.HasValue)
            {
                throw new ConcurrencyException(
                    "The cart already exists. Refresh the cart and send its concurrency token.");
            }

            CartApplicationRules.EnsureExpectedConcurrencyToken(
                cart,
                request.ExpectedConcurrencyToken);
        }

        CartApplicationRules.EnsureStockForAddition(cart, variant, request.Quantity);
        cart.AddItem(product.Id, variant.Id, request.Quantity, variant.Price);
        await _metricsRecorder.RecordAddedQuantityAsync(
            product,
            variant,
            request.Quantity,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedCart = await _cartRepository.GetByIdAsync(cart.Id, cancellationToken);
        return (savedCart ?? cart).ToDto();
    }
}
