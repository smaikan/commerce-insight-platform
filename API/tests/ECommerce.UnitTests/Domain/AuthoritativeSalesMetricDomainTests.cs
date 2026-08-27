using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class AuthoritativeSalesMetricDomainTests
{
    // Burada sipariş kaleminin Paid satışını yalnız bir kez ve tam adetle kaydettiğini doğruluyorum.
    [Fact]
    public void RecordPaidSale_Should_Be_Idempotent()
    {
        var item = CreateOrderItem(quantity: 3);

        item.RecordPaidSale().Should().Be(3);
        item.RecordPaidSale().Should().Be(0);

        item.PaidSalesQuantity.Should().Be(3);
        item.ReversedSalesQuantity.Should().Be(0);
    }

    // Burada kısmi ters işlemlerin yalnız satılmış kalan adede kadar ve tekrar güvenli uygulandığını doğruluyorum.
    [Fact]
    public void ReversePaidSale_Should_Apply_Only_The_Remaining_Paid_Quantity()
    {
        var item = CreateOrderItem(quantity: 3);
        item.RecordPaidSale();

        item.ReversePaidSale(1).Should().Be(1);
        item.ReversePaidSale(3).Should().Be(2);
        item.ReversePaidSale(1).Should().Be(0);

        item.ReversedSalesQuantity.Should().Be(3);
    }

    // Burada ürün net satış sayacının negatif değere indirilemediğini doğruluyorum.
    [Fact]
    public void NetSalesQuantity_Should_Not_Become_Negative()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN");
        product.IncreaseNetSalesQuantity(2);

        Action act = () => product.DecreaseNetSalesQuantity(3);

        act.Should().Throw<DomainException>();
        product.NetSalesQuantity.Should().Be(2);
    }

    // Burada test sipariş kalemini geçerli fiyat toplamıyla hazırlıyorum.
    private static OrderItem CreateOrderItem(int quantity)
    {
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 10m * quantity, 0m, 0m, 0m, 10m * quantity);
        return order.AddItem(12, Guid.NewGuid(), "Product", "SKU", 10m, quantity);
    }
}
