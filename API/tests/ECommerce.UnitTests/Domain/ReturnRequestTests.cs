using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ReturnRequestTests
{
    // Burada refund talebinin sipariş snapshot tutarını koruyup onay-teslim-tamamlama geçişlerini uyguladığını doğruluyorum.
    [Fact]
    public void Refund_Request_Should_Calculate_Refund_Total_And_Complete_Its_Lifecycle()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-REFUND", ReturnType.Refund, " Defective item ");

        returnRequest.AddItem(item, 1);
        returnRequest.Approve(utcNow, " Approved ");
        returnRequest.Receive(utcNow.AddMinutes(1));
        returnRequest.Complete(utcNow.AddMinutes(2));

        returnRequest.CustomerNote.Should().Be("Defective item");
        returnRequest.DecisionNote.Should().Be("Approved");
        returnRequest.RefundTotal.Should().Be(10m);
        returnRequest.Status.Should().Be(ReturnRequestStatus.Completed);
        returnRequest.IsCompletedRefund().Should().BeTrue();
    }

    // Burada exchange talebinin replacement varyantı olmadan oluşturulmasını engellediğini doğruluyorum.
    [Fact]
    public void Exchange_Request_Should_Require_A_Different_Replacement_Variant()
    {
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-EXCHANGE", ReturnType.Exchange);

        Action missingReplacement = () => returnRequest.AddItem(item, 1);
        Action sameReplacement = () => returnRequest.AddItem(item, 1, item.ProductVariantId);

        missingReplacement.Should().Throw<DomainException>();
        sameReplacement.Should().Throw<DomainException>();
        returnRequest.Items.Should().BeEmpty();
    }

    // Burada iade tutarının ürün fiyatı yerine indirim ve vergi sonrası sipariş snapshot'ından geldiğini doğruluyorum.
    [Fact]
    public void Refund_Request_Should_Use_Tax_And_Discount_Aware_Item_Refund_Total()
    {
        var order = new Order(7, "ORD-RETURN-TAX", 100m, 10m, 0m, 18m, 108m);
        var item = order.AddItem(
            12,
            Guid.NewGuid(),
            "Return Product",
            "RETURN-TAX-SKU",
            100m,
            1,
            discountTotal: 10m,
            taxRatePercentage: 20m,
            taxTotal: 18m);
        order.EnsureItemsMatchSubTotal();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-TAX", ReturnType.Refund);

        returnRequest.AddItem(item, 1);

        returnRequest.RefundTotal.Should().Be(108m);
        returnRequest.Items.Single().RefundTotal.Should().Be(108m);
    }

    // Burada testin iade aggregate kurallarını çalıştıracağı tek kalemli siparişi hazırlıyorum.
    private static (Order Order, OrderItem Item) CreateOrderWithItem()
    {
        var order = new Order(7, "ORD-RETURN", 10m, 0m, 0m, 0m, 10m);
        var item = order.AddItem(12, Guid.NewGuid(), "Return Product", "RETURN-SKU", 10m, 1);
        order.EnsureItemsMatchSubTotal();
        return (order, item);
    }
}
