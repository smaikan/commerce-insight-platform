using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class OrderCancellationOperationTests
{
    // Burada aynı gün cancel operasyonunun süresi dolmuş Processing lease'inden worker tarafından güvenle devralınabildiğini doğruluyorum.
    [Fact]
    public void TryClaim_Should_Reclaim_Expired_Processing_Lease()
    {
        var (order, payment) = CreatePaidOrderWithProviderItems();
        var now = DateTime.UtcNow;
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            now);

        operation.TryClaim(now, TimeSpan.FromMinutes(2)).Should().BeTrue();
        operation.TryClaim(now.AddMinutes(1), TimeSpan.FromMinutes(2)).Should().BeFalse();
        operation.TryClaim(now.AddMinutes(3), TimeSpan.FromMinutes(2)).Should().BeTrue();

        operation.Status.Should().Be(OrderCancellationOperationStatus.Processing);
        operation.AttemptCount.Should().Be(2);
    }

    // Burada crash öncesinde Processing kalan item refund kaydının yeni lease altında yeniden uzlaştırılabildiğini doğruluyorum.
    [Fact]
    public void ClaimNextRefundItem_Should_Reclaim_Stale_Processing_Item()
    {
        var (order, payment) = CreatePaidOrderWithProviderItems();
        var now = DateTime.UtcNow;
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Guest,
            PaymentReversalType.Refund,
            now);

        operation.TryClaim(now, TimeSpan.FromMinutes(2)).Should().BeTrue();
        var firstClaim = operation.ClaimNextRefundItem(now);
        firstClaim.Should().NotBeNull();
        operation.MarkReconciliationPending(
            now,
            now.AddMinutes(3),
            "worker_interrupted",
            "Worker processing was interrupted.");
        operation.TryClaim(now.AddMinutes(4), TimeSpan.FromMinutes(2)).Should().BeTrue();

        var reclaimed = operation.ClaimNextRefundItem(now.AddMinutes(4));

        reclaimed.Should().BeSameAs(firstClaim);
        reclaimed!.Status.Should().Be(PaymentReversalItemStatus.Processing);
    }

    // Burada çok kalemli refund operasyonunun provider paidPrice dağılımını tahmin etmeden birebir kullandığını doğruluyorum.
    [Fact]
    public void Refund_Operation_Should_Use_Provider_Item_Paid_Amounts()
    {
        var (order, payment) = CreatePaidOrderWithProviderItems();
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Refund,
            DateTime.UtcNow);

        operation.Items.Should().HaveCount(2);
        operation.Items.Sum(item => item.Amount).Should().Be(110m);
        operation.Items.Select(item => item.Amount).Should().BeEquivalentTo([44m, 66m]);
        operation.Items.Select(item => item.ProviderPaymentTransactionId)
            .Should().OnlyHaveUniqueItems();
    }

    // Burada teknik manual-review kaydının açık bir kararla yeniden mutabakat kuyruğuna alınabildiğini doğruluyorum.
    [Fact]
    public void RequeueManualReview_Should_Clear_Terminal_Error_And_Be_Claimable()
    {
        var (order, payment) = CreatePaidOrderWithProviderItems();
        var now = DateTime.UtcNow;
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            now);
        operation.TryClaim(now, TimeSpan.FromMinutes(2)).Should().BeTrue();
        operation.MarkManualReview(now, "provider_response_mismatch", "Provider report could not be verified.");

        operation.RequeueManualReview(now.AddSeconds(1));

        operation.Status.Should().Be(OrderCancellationOperationStatus.ReconciliationPending);
        operation.ErrorCode.Should().BeNull();
        operation.ErrorSummary.Should().BeNull();
        operation.TryClaim(now.AddSeconds(1), TimeSpan.FromMinutes(2)).Should().BeTrue();
    }

    // Burada gerçek provider kalemleri bulunan tahsil edilmiş iki ürünlü sipariş aggregate'ını hazırlıyorum.
    private static (Order Order, Payment Payment) CreatePaidOrderWithProviderItems()
    {
        var product = new Product(
            "Cancellation Product",
            $"cancellation-product-{Guid.NewGuid():N}",
            $"CANCEL-{Guid.NewGuid():N}"[..30],
            status: ProductStatus.Active)
            .WithId(901);
        var firstVariant = new ProductVariant(product.Id, "First", $"CAN-A-{Guid.NewGuid():N}"[..30], 40m, 5);
        var secondVariant = new ProductVariant(product.Id, "Second", $"CAN-B-{Guid.NewGuid():N}"[..30], 60m, 5);
        var order = new Order(
            7,
            $"ORD-{Guid.NewGuid():N}"[..24],
            100m,
            0m,
            0m,
            0m,
            100m);
        order.AddItem(product.Id, firstVariant.Id, product.Title, firstVariant.Sku, 40m, 1);
        order.AddItem(product.Id, secondVariant.Id, product.Title, secondVariant.Sku, 60m, 1);
        order.EnsureItemsMatchSubTotal();
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, 100m, "cancel_operation_test_key");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            $"token-{Guid.NewGuid():N}",
            payment.Id.ToString("N"),
            $"https://sandbox-cpp.iyzipay.com?token={Guid.NewGuid():N}",
            DateTime.UtcNow.AddMinutes(30));
        payment.MarkAsPaid("provider-payment-901", 1, 110m, 3);
        payment.RecordProviderItemTransactions(
            order.Items.OrderBy(item => item.Id).Select((item, index) =>
                new ProviderPaymentItemSnapshot(
                    item.Id,
                    $"provider-item-{index + 1}",
                    item.TotalPrice,
                    index == 0 ? 44m : 66m)).ToList(),
            DateTime.UtcNow);
        return (order, payment);
    }
}
