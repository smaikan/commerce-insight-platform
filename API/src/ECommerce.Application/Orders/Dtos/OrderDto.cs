using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Orders.Dtos;

// Burada oluşturulan siparişin güvenli özet cevabını tanımlıyorum.
public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? CouponCode,
    string? ShippingMethodName,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments,
    OrderAddressDto? ShippingAddress,
    DateTime? ReservationExpiresAt,
    DateTime? PaidAt,
    DateTime? CancelledAt,
    DateTime CreatedAt);

// Burada sipariş kaleminin snapshot bilgilerini tanımlıyorum.
public sealed record OrderItemDto(
    Guid Id,
    string ProductId,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantSku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    decimal DiscountTotal,
    decimal? TaxRatePercentage,
    decimal TaxTotal,
    decimal RefundTotal);

// Burada sipariş listesinin PII ve kalem grafiği taşımayan hafif cevap modelini tanımlıyorum.
public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal GrandTotal,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? PaidAt);

// Burada sipariş sahibine gösterilecek değişmez teslimat adresi snapshot'ını tanımlıyorum.
public sealed record OrderAddressDto(
    Guid SourceAddressId,
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string FullAddress,
    string? PostalCode);

// Burada ödeme denemesinin güvenli durum ve sağlayıcı özetini tanımlıyorum.
public sealed record PaymentDto(
    Guid Id,
    PaymentProvider Provider,
    PaymentStatus Status,
    decimal Amount,
    string? TransactionId,
    DateTime? PaidAt,
    DateTime CreatedAt);

public static class OrderDtoMapping
{
    // Burada ödeme aggregate'ını istemciye güvenle verilecek ödeme DTO'suna dönüştürüyorum.
    public static PaymentDto ToDto(this Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.Provider,
            payment.Status,
            payment.Amount,
            payment.TransactionId,
            payment.PaidAt,
            payment.CreatedAt);
    }

    // Burada sipariş aggregate'ını dışarıya güvenle verilecek cevap modeline dönüştürüyorum.
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.SubTotal,
            order.DiscountTotal,
            order.ShippingTotal,
            order.TaxTotal,
            order.GrandTotal,
            order.CouponCode,
            order.ShippingMethodName,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemDto(
                    item.Id,
                    PublicIdCodec.EncodeProductId(item.ProductId),
                    item.ProductVariantId,
                    item.ProductTitleSnapshot,
                    item.VariantSkuSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.TotalPrice,
                    item.DiscountTotal,
                    item.TaxRatePercentage,
                    item.TaxTotal,
                    item.RefundTotal))
                .ToList(),
            order.Payments
                .OrderBy(payment => payment.CreatedAt)
                .ThenBy(payment => payment.Id)
                .Select(payment => new PaymentDto(
                    payment.Id,
                    payment.Provider,
                    payment.Status,
                    payment.Amount,
                    payment.TransactionId,
                    payment.PaidAt,
                    payment.CreatedAt))
                .ToList(),
            order.ShippingAddressSnapshot is null
                ? null
                : new OrderAddressDto(
                    order.ShippingAddressSnapshot.SourceAddressId,
                    order.ShippingAddressSnapshot.Title,
                    order.ShippingAddressSnapshot.FirstName,
                    order.ShippingAddressSnapshot.LastName,
                    order.ShippingAddressSnapshot.PhoneNumber,
                    order.ShippingAddressSnapshot.City,
                    order.ShippingAddressSnapshot.District,
                    order.ShippingAddressSnapshot.FullAddress,
                    order.ShippingAddressSnapshot.PostalCode),
            order.ReservationExpiresAt,
            order.PaidAt,
            order.CancelledAt,
            order.CreatedAt);
    }

    // Burada sipariş aggregate'ını listeler için küçük ve PII içermeyen özet DTO'ya dönüştürüyorum.
    public static OrderSummaryDto ToSummaryDto(this Order order)
    {
        return new OrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.GrandTotal,
            order.Items.Count,
            order.CreatedAt,
            order.PaidAt);
    }
}
