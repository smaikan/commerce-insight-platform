using ECommerce.Domain.Entities;

namespace ECommerce.Application.ShippingMethods.Dtos;

// Burada kargo yönteminin checkout seçimi ve yönetim ekranlarında kullanılacak cevap modelini tanımlıyorum.
public sealed record ShippingMethodDto(
    Guid Id,
    string Name,
    decimal FixedFee,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class ShippingMethodDtoMapping
{
    // Burada domain kargo yöntemini dış katmanlara taşınabilecek DTO modeline dönüştürüyorum.
    public static ShippingMethodDto ToDto(this ShippingMethod shippingMethod)
    {
        return new ShippingMethodDto(
            shippingMethod.Id,
            shippingMethod.Name,
            shippingMethod.FixedFee,
            shippingMethod.IsActive,
            shippingMethod.DisplayOrder,
            shippingMethod.CreatedAt,
            shippingMethod.UpdatedAt);
    }
}
