using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class OrderReservationTests
{
    // Burada ödeme bekleyen siparişin stok rezervasyonunu süre dolduğunda güvenle iptal edebildiğini doğruluyorum.
    [Fact]
    public void ExpireStockReservation_Should_Cancel_An_Unpaid_Expired_Order()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var order = CreateOrderWithItem();
        order.StartStockReservation(utcNow, TimeSpan.FromMinutes(15));

        var expired = order.ExpireStockReservation(utcNow.AddMinutes(15));

        expired.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(utcNow.AddMinutes(15));
    }

    // Burada sağlayıcı sonucu belirsiz bekleyen ödeme varken rezervasyonun otomatik iptal edilmediğini doğruluyorum.
    [Fact]
    public void ExpireStockReservation_Should_Not_Cancel_An_Order_With_A_Pending_Payment()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var order = CreateOrderWithItem();
        order.StartStockReservation(utcNow, TimeSpan.FromMinutes(15));
        order.AddPayment(new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "reservation_pending_payment_01"));

        var expired = order.ExpireStockReservation(utcNow.AddMinutes(16));

        expired.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    // Burada testin stok rezervasyonu kurallarını çalıştıracağı tek kalemli ödeme bekleyen siparişi hazırlıyorum.
    private static Order CreateOrderWithItem()
    {
        var order = new Order(7, "ORD-RESERVATION", 10m, 0m, 0m, 0m, 10m);
        order.AddItem(12, Guid.NewGuid(), "Reservation Product", "RESERVATION-SKU", 10m, 1);
        order.EnsureItemsMatchSubTotal();
        return order;
    }
}
