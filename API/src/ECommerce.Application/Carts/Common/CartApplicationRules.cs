using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Carts.Common;

internal static class CartApplicationRules
{
    // Burada sepete eklenecek ürün ve varyantın birbiriyle uyumlu ve satışa açık olduğunu doğruluyorum.
    public static void EnsurePurchasable(Product product, ProductVariant variant)
    {
        if (variant.ProductId != product.Id)
        {
            throw new ConflictException("Product variant does not belong to the product.");
        }

        if (!product.IsActive || product.Status != ProductStatus.Active)
        {
            throw new ConflictException("Product is not available for sale.");
        }

        if (!variant.IsActive)
        {
            throw new ConflictException("Product variant is not available for sale.");
        }
    }

    // Burada yeni eklemeyle oluşacak toplam varyant adedinin güncel stok içinde kaldığını doğruluyorum.
    public static void EnsureStockForAddition(
        Cart cart,
        ProductVariant variant,
        int addedQuantity)
    {
        var currentQuantity = cart.Items
            .SingleOrDefault(item => item.ProductVariantId == variant.Id)
            ?.Quantity ?? 0;
        var requestedQuantity = (long)currentQuantity + addedQuantity;

        if (requestedQuantity > variant.Stock)
        {
            throw new ConflictException("Requested cart quantity exceeds available stock.");
        }
    }

    // Burada doğrudan yazılacak sepet adedinin güncel stok içinde kaldığını doğruluyorum.
    public static void EnsureStockForQuantity(ProductVariant variant, int quantity)
    {
        if (quantity > variant.Stock)
        {
            throw new ConflictException("Requested cart quantity exceeds available stock.");
        }
    }

    // Burada istemcinin gönderdiği eski concurrency tokenıyla sepetin ezilmesini engelliyorum.
    public static void EnsureExpectedConcurrencyToken(
        Cart cart,
        Guid? expectedConcurrencyToken)
    {
        if (expectedConcurrencyToken.HasValue &&
            cart.ConcurrencyToken != expectedConcurrencyToken.Value)
        {
            throw new ConcurrencyException(
                "The cart was changed by another operation. Refresh the cart and try again.");
        }
    }

    // Burada işlem yapılacak satırın gerçekten çözümlenen sepete ait olduğunu doğruluyorum.
    public static CartItem GetOwnedItem(Cart cart, Guid cartItemId)
    {
        return cart.Items.SingleOrDefault(item => item.Id == cartItemId)
            ?? throw new NotFoundException("Cart item was not found.");
    }
}
