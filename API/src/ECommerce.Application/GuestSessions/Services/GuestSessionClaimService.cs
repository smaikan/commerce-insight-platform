using ECommerce.Application.Carts.Common;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.GuestSessions.Dtos;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.GuestSessions.Services;

public sealed class GuestSessionClaimService : IGuestSessionClaimService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductEngagementRepository _engagementRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada cart ve favorites claim işlemini aynı transaction'da yürütecek bağımlılıkları hazırlıyorum.
    public GuestSessionClaimService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IProductEngagementRepository engagementRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _engagementRepository = engagementRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada guest session verilerini tek serializable transaction içinde kullanıcıya claim ediyorum.
    public Task<GuestSessionClaimDto> ClaimAsync(
        long userId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var userOwner = CartOwner.ForUser(userId);
        var guestOwner = CartOwner.ForGuest(sessionId);
        var userFavoriteOwner = FavoriteOwner.ForUser(userId);
        var guestFavoriteOwner = FavoriteOwner.ForGuest(sessionId);

        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => ClaimInTransactionAsync(
                userOwner,
                guestOwner,
                userFavoriteOwner,
                guestFavoriteOwner,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada cart ve favorites önceliklerini birlikte uygulayıp tek kalıcı sonuç oluşturuyorum.
    private async Task<GuestSessionClaimDto> ClaimInTransactionAsync(
        CartOwner userOwner,
        CartOwner guestOwner,
        FavoriteOwner userFavoriteOwner,
        FavoriteOwner guestFavoriteOwner,
        CancellationToken cancellationToken)
    {
        var userCart = await _cartRepository.GetByOwnerForUpdateAsync(userOwner, cancellationToken);
        var guestCart = await _cartRepository.GetByOwnerForUpdateAsync(guestOwner, cancellationToken);
        var cartClaim = ClaimCart(userOwner.UserId!.Value, userCart, guestCart);

        var userFavoriteCount = await _engagementRepository.CountFavoritesAsync(
            userFavoriteOwner,
            cancellationToken);
        var guestFavorites = await _engagementRepository.GetFavoritesForUpdateAsync(
            guestFavoriteOwner,
            cancellationToken);
        var favoriteClaim = await ClaimFavoritesAsync(
            userFavoriteOwner.UserId!.Value,
            userFavoriteCount,
            guestFavorites,
            cancellationToken);

        if (cartClaim.Changed || favoriteClaim.Changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var cart = cartClaim.Cart;
        if (cart is null)
        {
            return new GuestSessionClaimDto(CartDto.Empty(), favoriteClaim.FavoriteCount);
        }

        var persistedCart = cartClaim.Changed
            ? await _cartRepository.GetByIdAsync(cart.Id, cancellationToken)
            : null;
        return new GuestSessionClaimDto(
            (persistedCart ?? cart).ToDto(),
            favoriteClaim.FavoriteCount);
    }

    // Burada boş kullanıcı sepetine guest içeriğini devrediyor, dolu kullanıcı sepetinde guest içeriğini siliyorum.
    private CartClaimResult ClaimCart(long userId, Cart? userCart, Cart? guestCart)
    {
        if (guestCart is null)
        {
            return new CartClaimResult(userCart, false);
        }

        if (userCart is null)
        {
            RefreshGuestCart(guestCart);
            guestCart.AssignToUser(userId);
            return new CartClaimResult(guestCart, true);
        }

        if (userCart.IsEmpty)
        {
            foreach (var guestItem in ValidateGuestCartItems(guestCart))
            {
                userCart.AddItem(
                    guestItem.Product.Id,
                    guestItem.Variant.Id,
                    guestItem.Item.Quantity,
                    guestItem.Variant.Price);
            }
        }

        _cartRepository.Remove(guestCart);
        return new CartClaimResult(userCart, true);
    }

    // Burada üyenin favorisi yoksa guest kayıtlarını devrediyor, varsa üye listesini koruyup guest kayıtlarını siliyorum.
    private async Task<FavoriteClaimResult> ClaimFavoritesAsync(
        long userId,
        int userFavoriteCount,
        IReadOnlyList<FavoriteProduct> guestFavorites,
        CancellationToken cancellationToken)
    {
        if (guestFavorites.Count == 0)
        {
            return new FavoriteClaimResult(userFavoriteCount, false);
        }

        if (userFavoriteCount == 0)
        {
            foreach (var favorite in guestFavorites)
            {
                favorite.AssignToUser(userId);
            }

            return new FavoriteClaimResult(guestFavorites.Count, true);
        }

        var products = await _productRepository.GetByIdsForUpdateAsync(
            guestFavorites.Select(favorite => favorite.ProductId),
            cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        foreach (var favorite in guestFavorites)
        {
            if (productsById.TryGetValue(favorite.ProductId, out var product))
            {
                product.DecreaseFavoriteCount();
            }

            _engagementRepository.RemoveFavorite(favorite);
        }

        return new FavoriteClaimResult(userFavoriteCount, true);
    }

    // Burada kullanıcıya devredilecek guest sepet satırlarının güncel fiyat ve stok kurallarını doğruluyorum.
    private static IReadOnlyList<(CartItem Item, Product Product, ProductVariant Variant)> ValidateGuestCartItems(
        Cart guestCart)
    {
        var validatedItems = new List<(CartItem, Product, ProductVariant)>();

        foreach (var guestItem in guestCart.Items.ToList())
        {
            var product = guestItem.Product
                ?? throw new NotFoundException("Product was not found.");
            var variant = guestItem.ProductVariant
                ?? throw new NotFoundException("Product variant was not found.");

            CartApplicationRules.EnsurePurchasable(product, variant);
            CartApplicationRules.EnsureStockForQuantity(variant, guestItem.Quantity);
            validatedItems.Add((guestItem, product, variant));
        }

        return validatedItems;
    }

    // Burada doğrudan devredilecek guest sepetinin snapshot fiyatlarını güncel katalog fiyatıyla yeniliyorum.
    private static void RefreshGuestCart(Cart guestCart)
    {
        foreach (var guestItem in ValidateGuestCartItems(guestCart))
        {
            guestCart.UpdateItem(
                guestItem.Item.Id,
                guestItem.Item.Quantity,
                guestItem.Variant.Price);
        }
    }

    // Burada cart claim sonucunu seçilen aggregate ve değişiklik bilgisiyle taşıyorum.
    private sealed record CartClaimResult(Cart? Cart, bool Changed);

    // Burada favorite claim sonucunu güncel sayı ve değişiklik bilgisiyle taşıyorum.
    private sealed record FavoriteClaimResult(int FavoriteCount, bool Changed);
}
