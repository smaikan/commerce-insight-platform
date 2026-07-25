using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Carts.Commands.MergeGuestCart;

public sealed class MergeGuestCartCommandHandler
    : IRequestHandler<MergeGuestCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada misafir sepeti birleştirmesi için kullanıcı, repository ve transaction bağımlılıklarını hazırlıyorum.
    public MergeGuestCartCommandHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada misafir ve kullanıcı sepetlerini tek serializable transaction içinde birleştiriyorum.
    public Task<CartDto> Handle(
        MergeGuestCartCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => MergeInTransactionAsync(
                userId,
                request.SessionId,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada misafir satırlarını güncel fiyat ve stokla birleştirip eski misafir sepetini kaldırıyorum.
    private async Task<CartDto> MergeInTransactionAsync(
        long userId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var userOwner = CartOwner.ForUser(userId);
        var guestOwner = CartOwner.ForGuest(sessionId);
        var userCart = await _cartRepository.GetByOwnerForUpdateAsync(
            userOwner,
            cancellationToken);
        var guestCart = await _cartRepository.GetByOwnerForUpdateAsync(
            guestOwner,
            cancellationToken);

        if (guestCart is null)
        {
            return userCart?.ToDto() ?? CartDto.Empty();
        }

        var guestItems = ValidateGuestCartItems(guestCart, userCart);
        if (userCart is null)
        {
            foreach (var guestItem in guestItems)
            {
                guestCart.UpdateItem(
                    guestItem.Item.Id,
                    guestItem.Item.Quantity,
                    guestItem.Variant.Price);
            }

            guestCart.AssignToUser(userId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var assignedCart = await _cartRepository.GetByIdAsync(
                guestCart.Id,
                cancellationToken);
            return (assignedCart ?? guestCart).ToDto();
        }

        foreach (var guestItem in guestItems)
        {
            userCart.AddItem(
                guestItem.Product.Id,
                guestItem.Variant.Id,
                guestItem.Item.Quantity,
                guestItem.Variant.Price);
        }

        _cartRepository.Remove(guestCart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var mergedCart = await _cartRepository.GetByIdAsync(
            userCart.Id,
            cancellationToken);
        return (mergedCart ?? userCart).ToDto();
    }

    // Burada misafir sepetinin tüm satırlarını state'i değiştirmeden önce katalog ve stok kurallarıyla doğruluyorum.
    private static IReadOnlyList<(
        CartItem Item,
        Product Product,
        ProductVariant Variant)> ValidateGuestCartItems(
            Cart guestCart,
            Cart? userCart)
    {
        if (userCart is not null)
        {
            var newDistinctItemCount = guestCart.Items.Count(
                guestItem => userCart.Items.All(
                    userItem => userItem.ProductVariantId != guestItem.ProductVariantId));
            if ((long)userCart.Items.Count + newDistinctItemCount >
                Cart.MaximumDistinctItemCount)
            {
                throw new ConflictException(
                    $"Cart cannot contain more than {Cart.MaximumDistinctItemCount} distinct items.");
            }
        }

        var validatedItems = new List<(CartItem, Product, ProductVariant)>();

        foreach (var guestItem in guestCart.Items.ToList())
        {
            var product = guestItem.Product
                ?? throw new NotFoundException("Product was not found.");
            var variant = guestItem.ProductVariant
                ?? throw new NotFoundException("Product variant was not found.");

            CartApplicationRules.EnsurePurchasable(product, variant);
            if (userCart is null)
            {
                CartApplicationRules.EnsureStockForQuantity(variant, guestItem.Quantity);
            }
            else
            {
                CartApplicationRules.EnsureStockForAddition(
                    userCart,
                    variant,
                    guestItem.Quantity);
            }

            validatedItems.Add((guestItem, product, variant));
        }

        return validatedItems;
    }
}
