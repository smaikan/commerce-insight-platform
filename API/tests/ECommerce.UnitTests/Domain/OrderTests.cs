using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class OrderTests
{
    // Burada sipariş oluşturulurken grand total formülünün bozulmasını engelliyorum.
    [Fact]
    public void Constructor_Should_Reject_Inconsistent_Grand_Total()
    {
        Action act = () => new Order(1, "ORD-1", 100m, 10m, 5m, 20m, 999m);

        act.Should().Throw<DomainException>();
    }

    // Burada sipariş kaleminin güvenilir snapshot verisiyle subtotal değerini tam olarak oluşturduğunu doğruluyorum.
    [Fact]
    public void AddItem_Should_Create_Immutable_Snapshot_And_Match_SubTotal()
    {
        var order = new Order(1, "ORD-1", 200m, 0m, 0m, 0m, 200m);
        var variantId = Guid.NewGuid();

        var item = order.AddItem(
            12,
            variantId,
            "  Product title  ",
            " sku-1 ",
            100m,
            2,
            productUrlSnapshot: " product-title ",
            imageUrlSnapshot: " https://cdn.example.com/product.jpg ",
            imageAltSnapshot: " Product image ",
            variantNameSnapshot: " Renk ",
            variantValueSnapshot: " Pudra ");
        order.EnsureItemsMatchSubTotal();

        item.OrderId.Should().Be(order.Id);
        item.ProductTitleSnapshot.Should().Be("Product title");
        item.VariantSkuSnapshot.Should().Be("sku-1");
        item.ProductUrlSnapshot.Should().Be("product-title");
        item.ImageUrlSnapshot.Should().Be("https://cdn.example.com/product.jpg");
        item.ImageAltSnapshot.Should().Be("Product image");
        item.VariantNameSnapshot.Should().Be("Renk");
        item.VariantValueSnapshot.Should().Be("Pudra");
        item.TotalPrice.Should().Be(200m);
        order.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    // Burada sipariş kaleminin indirim, vergi ve seçilmiş kargo yöntemi snapshot'larını sipariş toplamıyla birlikte koruduğunu doğruluyorum.
    [Fact]
    public void AddItem_Should_Preserve_Tax_Discount_And_Shipping_Snapshots()
    {
        var shippingMethodId = Guid.NewGuid();
        var order = new Order(
            1,
            "ORD-TAX-SHIPPING",
            100m,
            10m,
            15m,
            18m,
            123m,
            shippingMethodId: shippingMethodId,
            shippingMethodName: "Express");

        var item = order.AddItem(
            12,
            Guid.NewGuid(),
            "Product title",
            "SKU-1",
            100m,
            1,
            discountTotal: 10m,
            taxRatePercentage: 20m,
            taxTotal: 18m);

        order.EnsureItemsMatchSubTotal();

        order.ShippingMethodId.Should().Be(shippingMethodId);
        order.ShippingMethodName.Should().Be("Express");
        item.DiscountTotal.Should().Be(10m);
        item.TaxRatePercentage.Should().Be(20m);
        item.TaxTotal.Should().Be(18m);
        item.RefundTotal.Should().Be(108m);
    }

    // Burada sipariş kalemleri toplamı subtotal ile eşleşmiyorsa siparişin tamamlanmasını engelliyorum.
    [Fact]
    public void EnsureItemsMatchSubTotal_Should_Reject_Incomplete_Item_Total()
    {
        var order = new Order(1, "ORD-1", 200m, 0m, 0m, 0m, 200m);
        order.AddItem(12, Guid.NewGuid(), "Product title", "SKU-1", 100m, 1);

        Action act = () => order.EnsureItemsMatchSubTotal();

        act.Should().Throw<DomainException>();
    }

    // Burada aynı varyantın bir siparişe ikinci kez ayrı kalem olarak eklenmesini engelliyorum.
    [Fact]
    public void AddItem_Should_Reject_Duplicate_Product_Variant()
    {
        var order = new Order(1, "ORD-1", 200m, 0m, 0m, 0m, 200m);
        var variantId = Guid.NewGuid();
        order.AddItem(12, variantId, "Product title", "SKU-1", 100m, 1);

        Action act = () => order.AddItem(12, variantId, "Product title", "SKU-1", 100m, 1);

        act.Should().Throw<DomainException>();
        order.Items.Should().ContainSingle();
    }

    // Burada sipariş durumunun geçerli yaşam döngüsünde paid ve delivered tarihlerini doğru tuttuğunu doğruluyorum.
    [Fact]
    public void ChangeStatus_Should_Allow_Valid_Order_Lifecycle()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-1", 100m, 0m, 0m, 0m, 100m);

        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "order_lifecycle_payment_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("fake_order_lifecycle_transaction_001");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));

        order.Status.Should().Be(OrderStatus.Delivered);
        order.PaidAt.Should().Be(utcNow.AddMinutes(1));
        order.ShippedAt.Should().Be(utcNow.AddMinutes(3));
        order.DeliveredAt.Should().Be(utcNow.AddMinutes(4));
    }

    // Burada kargo takip snapshot'ının güvenli URL ile saklanıp ilk kargoya çıkış zamanını koruduğunu doğruluyorum.
    [Fact]
    public void SetShipment_Should_Persist_Tracking_And_Preserve_First_Shipped_Time()
    {
        var utcNow = new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Utc);
        var order = CreatePreparingOrder(utcNow);

        order.SetShipment("  Yurtiçi Kargo  ", "  TRACK-123  ", "https://track.example.com/TRACK-123", utcNow.AddMinutes(3));
        order.SetShipment("Yurtiçi Kargo", "TRACK-456", null, utcNow.AddMinutes(4));

        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippingCarrier.Should().Be("Yurtiçi Kargo");
        order.TrackingNumber.Should().Be("TRACK-456");
        order.TrackingUrl.Should().BeNull();
        order.ShippedAt.Should().Be(utcNow.AddMinutes(3));
    }

    // Burada güvenli olmayan takip protokolünün siparişe kaydedilmesini engellediğimi doğruluyorum.
    [Fact]
    public void SetShipment_Should_Reject_Unsafe_Tracking_Url()
    {
        var utcNow = new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Utc);
        var order = CreatePreparingOrder(utcNow);

        Action act = () => order.SetShipment("Carrier", "TRACK-123", "javascript:alert(1)", utcNow.AddMinutes(3));

        act.Should().Throw<DomainException>();
        order.Status.Should().Be(OrderStatus.Preparing);
    }

    // Burada başarılı ödeme kaydı olmadan parasal siparişin paid durumuna geçirilmesini engelliyorum.
    [Fact]
    public void ChangeStatus_Should_Reject_Paid_Without_A_Successful_Payment()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-1", 100m, 0m, 0m, 0m, 100m);
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);

        Action act = () => order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));

        act.Should().Throw<DomainException>();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    // Burada ödeme idempotency anahtarının veritabanı karşılaştırmasıyla uyumlu kanonik büyük harfli biçimde saklandığını doğruluyorum.
    [Fact]
    public void Payment_Should_Normalize_The_Idempotency_Key()
    {
        var payment = new Payment(Guid.NewGuid(), PaymentProvider.Fake, 10m, "mixed_Key-001");

        payment.IdempotencyKey.Should().Be("MIXED_KEY-001");
    }

    // Burada UTC olmayan zamanla durum geçişi yapılmasını engelliyorum.
    [Fact]
    public void ChangeStatus_Should_Require_Utc_Time()
    {
        var order = new Order(1, "ORD-1", 100m, 0m, 0m, 0m, 100m);

        Action act = () => order.ChangeStatus(OrderStatus.Confirmed, DateTime.Now);

        act.Should().Throw<DomainException>();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    // Burada geçersiz sipariş durum geçişini reddediyorum.
    [Fact]
    public void ChangeStatus_Should_Reject_Invalid_Transition()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-1", 100m, 0m, 0m, 0m, 100m);

        Action act = () => order.ChangeStatus(OrderStatus.Delivered, utcNow);

        act.Should().Throw<DomainException>();
    }

    // Burada kargoya verilmeden önceki Pending, Confirmed, Paid ve Preparing durumlarında iptale izin verildiğini doğruluyorum.
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Preparing)]
    public void ChangeStatus_Should_Allow_Cancellation_Before_Shipped(OrderStatus initialStatus)
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-CANCEL", 100m, 0m, 0m, 0m, 100m);

        if (initialStatus >= OrderStatus.Confirmed)
        {
            order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        }
        if (initialStatus >= OrderStatus.Paid)
        {
            var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "cancel_payment_key_001");
            order.AddPayment(payment);
            payment.MarkAsPaid("fake_cancel_tx_001");
            order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        }
        if (initialStatus >= OrderStatus.Preparing)
        {
            order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        }

        order.ChangeStatus(OrderStatus.Cancelled, utcNow.AddMinutes(3));
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(utcNow.AddMinutes(3));
    }

    // Burada kargoya verildikten sonra doğrudan iptalin reddedildiğini doğruluyorum.
    [Fact]
    public void ChangeStatus_Should_Reject_Cancellation_After_Shipped()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = CreatePreparingOrder(utcNow);
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));

        Action act = () => order.ChangeStatus(OrderStatus.Cancelled, utcNow.AddMinutes(4));

        act.Should().Throw<DomainException>();
    }

    // Burada kargo testleri için başarılı ödemeden hazırlama aşamasına kadar geçerli sipariş yaşam döngüsünü kuruyorum.
    private static Order CreatePreparingOrder(DateTime utcNow)
    {
        var order = new Order(1, "ORD-SHIPMENT", 100m, 0m, 0m, 0m, 100m);
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "shipment_payment_key_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("shipment_transaction_001");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        return order;
    }
}
