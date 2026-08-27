using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class AuthoritativeSalesMetricServiceTests
{
    // Burada Paid siparişin ürün adetlerini topladığını ve tekrarlanan callback benzeri çağrıda çift artırmadığını doğruluyorum.
    [Fact]
    public async Task RecordPaidOrder_Should_Aggregate_Product_Quantity_Only_Once()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN").WithId(12);
        var order = CreatePaidOrder(product.Id, 3);
        var service = CreateService(product);

        await service.RecordPaidOrderAsync(order);
        await service.RecordPaidOrderAsync(order);

        product.NetSalesQuantity.Should().Be(3);
        order.Items.Single().PaidSalesQuantity.Should().Be(3);
    }

    // Burada onaylı kısmi refund'ın yalnız ilgili adedi düşürdüğünü ve retry'da çift azaltmadığını doğruluyorum.
    [Fact]
    public async Task ReverseApprovedRefund_Should_Decrease_Only_Returned_Quantity_Once()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN").WithId(12);
        var order = CreatePaidOrder(product.Id, 3);
        var deliveredAt = order.PaidAt!.Value.AddMinutes(3);
        order.ChangeStatus(OrderStatus.Preparing, order.PaidAt.Value.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Shipped, order.PaidAt.Value.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Delivered, deliveredAt);
        var item = order.Items.Single();
        var request = new ReturnRequest(order.Id, order.UserId, "RET-100", ReturnType.Refund);
        request.AddItem(item, 1);
        request.Receive(deliveredAt.AddMinutes(1));
        request.Approve(deliveredAt.AddMinutes(2));
        var service = CreateService(product);
        await service.RecordPaidOrderAsync(order);

        await service.ReverseApprovedRefundAsync(order, request);
        await service.ReverseApprovedRefundAsync(order, request);

        product.NetSalesQuantity.Should().Be(2);
        item.ReversedSalesQuantity.Should().Be(1);
        request.Items.Single().SalesMetricReversedQuantity.Should().Be(1);
    }

    // Burada kesin sipariş iptalinin bütün kalemleri yalnız bir kez satış metriğinden çıkardığını doğruluyorum.
    [Fact]
    public async Task ReverseCancelledOrder_Should_Be_Idempotent()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN").WithId(12);
        var order = CreatePaidOrder(product.Id, 2);
        var service = CreateService(product);
        await service.RecordPaidOrderAsync(order);

        await service.ReverseCancelledOrderAsync(order);
        await service.ReverseCancelledOrderAsync(order);

        product.NetSalesQuantity.Should().Be(0);
        order.Items.Single().ReversedSalesQuantity.Should().Be(2);
    }

    // Burada takipli ürün deposu üzerinden çalışan gerçek satış metriği servisini hazırlıyorum.
    private static AuthoritativeSalesMetricService CreateService(Product product)
    {
        var products = new Mock<IProductRepository>();
        products.Setup(repository => repository.GetByIdsForSalesMetricUpdateAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        return new AuthoritativeSalesMetricService(products.Object);
    }

    // Burada satış metriği testleri için Paid durumuna ulaşmış tek kalemli siparişi hazırlıyorum.
    private static Order CreatePaidOrder(long productId, int quantity)
    {
        var now = DateTime.UtcNow.AddMinutes(1);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 10m * quantity, 0m, 0m, 0m, 10m * quantity);
        order.AddItem(productId, Guid.NewGuid(), "Product", "SKU", 10m, quantity);
        order.ChangeStatus(OrderStatus.Confirmed, now);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal);
        order.AddPayment(payment);
        payment.MarkAsPaid($"test-paid-{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, now);
        return order;
    }
}
