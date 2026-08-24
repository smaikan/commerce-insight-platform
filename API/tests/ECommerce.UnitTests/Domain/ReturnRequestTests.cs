using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ReturnRequestTests
{
    // Burada refund talebinin sipariş snapshot tutarını koruyup teslim-onay yaşam döngüsünü uyguladığını doğruluyorum.
    [Fact]
    public void Refund_Request_Should_Calculate_Refund_Total_And_Approve_After_Receipt()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-REFUND", ReturnType.Refund, " Defective item ");

        returnRequest.AddItem(item, 1);
        returnRequest.Receive(utcNow);
        returnRequest.Approve(utcNow.AddMinutes(1), " Approved ");

        returnRequest.CustomerNote.Should().Be("Defective item");
        returnRequest.DecisionNote.Should().Be("Approved");
        returnRequest.RefundTotal.Should().Be(10m);
        returnRequest.Status.Should().Be(ReturnRequestStatus.Approved);
        returnRequest.ReceivedAt.Should().Be(utcNow);
        returnRequest.ApprovedAt.Should().Be(utcNow.AddMinutes(1));
        returnRequest.CompletedAt.Should().BeNull();
        returnRequest.IsCompletedRefund().Should().BeTrue();
    }

    // Burada yeni talebin fiziksel teslim alınmadan onay veya ret kararı alamadığını doğruluyorum.
    [Fact]
    public void Requested_Return_Should_Reject_Decision_Transitions_Before_Receipt()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-PRE-RECEIPT", ReturnType.Refund);
        returnRequest.AddItem(item, 1);

        Action approve = () => returnRequest.Approve(utcNow);
        Action reject = () => returnRequest.Reject(utcNow);

        approve.Should().Throw<ReturnStatusTransitionException>();
        reject.Should().Throw<ReturnStatusTransitionException>();
        returnRequest.Status.Should().Be(ReturnRequestStatus.Requested);
    }

    // Burada teslim alınmış karar bekleyen talebin reddedildiğinde stok etkisiz terminal duruma geçtiğini doğruluyorum.
    [Fact]
    public void Received_Return_Should_Allow_Rejection_And_Disallow_Legacy_Completion()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-REJECT", ReturnType.Refund);
        returnRequest.AddItem(item, 1);
        returnRequest.Receive(utcNow);

        Action complete = () => returnRequest.Complete(utcNow.AddMinutes(1));
        complete.Should().Throw<ReturnStatusTransitionException>();

        returnRequest.Reject(utcNow.AddMinutes(1), "Rejected after inspection");

        returnRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        returnRequest.RejectedAt.Should().Be(utcNow.AddMinutes(1));
        returnRequest.ReceivedAt.Should().Be(utcNow);
    }

    // Burada eski onay-önce kaydın teslim ve completion yolunun yalnız geriye dönük uyumluluk kapsamında sürdüğünü doğruluyorum.
    [Fact]
    public void Legacy_Approved_Return_Should_Still_Receive_And_Complete()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var (order, item) = CreateOrderWithItem();
        var returnRequest = new ReturnRequest(order.Id, 7, "RET-DOMAIN-LEGACY", ReturnType.Exchange);
        returnRequest.AddItem(item, 1, Guid.NewGuid());
        SetPrivateProperty(returnRequest, nameof(ReturnRequest.Status), ReturnRequestStatus.Approved);
        SetPrivateProperty(returnRequest, nameof(ReturnRequest.ApprovedAt), utcNow);

        returnRequest.Receive(utcNow.AddMinutes(1));
        returnRequest.Complete(utcNow.AddMinutes(2));

        returnRequest.Status.Should().Be(ReturnRequestStatus.Completed);
        returnRequest.RepresentsApprovedOutcome().Should().BeTrue();
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

    // Burada yalnız geçmiş yaşam döngüsü kaydını temsil etmek için private EF alanını test fixture'ında ayarlıyorum.
    private static void SetPrivateProperty<T>(ReturnRequest returnRequest, string propertyName, T value)
    {
        typeof(ReturnRequest).GetProperty(propertyName)!.SetValue(returnRequest, value);
    }
}
