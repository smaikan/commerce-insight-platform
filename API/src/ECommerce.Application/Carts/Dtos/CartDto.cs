using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts.Dtos;

// Burada sepet özetini satırlar, toplamlar ve concurrency bilgisiyle taşıyan cevap modelini tanımlıyorum.
public sealed record CartDto(
    Guid? Id,
    Guid? ConcurrencyToken,
    IReadOnlyList<CartItemDto> Items,
    long TotalQuantity,
    decimal SubTotal,
    bool HasUnavailableItems,
    bool HasPriceChanges,
    DateTime? CreatedAt,
    DateTime? UpdatedAt)
{
    // Burada henüz kalıcı sepeti olmayan sahip için boş sepet cevabı oluşturuyorum.
    public static CartDto Empty() =>
        new(null, null, [], 0, 0m, false, false, null, null);
}

public static class CartDtoMapping
{
    // Burada sepet aggregate'ını item özetleri ve güncel concurrency tokenıyla DTO'ya dönüştürüyorum.
    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(item => item.ToDto())
            .ToList();

        return new CartDto(
            cart.Id,
            cart.ConcurrencyToken,
            items,
            cart.TotalQuantity,
            cart.SubTotal,
            items.Any(item => !item.IsAvailable),
            items.Any(item => item.PriceChanged),
            cart.CreatedAt,
            cart.UpdatedAt);
    }
}
