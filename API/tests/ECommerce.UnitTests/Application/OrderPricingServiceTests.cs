using ECommerce.Application.Orders.Services;
using FluentAssertions;

namespace ECommerce.UnitTests.Application;

public sealed class OrderPricingServiceTests
{
    // Burada farklı vergi oranlı satırlarda kupon indiriminin oransal dağıtılıp verginin indirim sonrası hesaplandığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Allocate_Discount_And_Calculate_Tax_Per_Line()
    {
        var firstVariantId = Guid.NewGuid();
        var secondVariantId = Guid.NewGuid();
        var service = new OrderPricingService();

        var result = service.Calculate(
            [
                new OrderPricingLine(firstVariantId, 100m, 20m),
                new OrderPricingLine(secondVariantId, 100m, 10m)
            ],
            discountTotal: 30m,
            shippingTotal: 15m);

        result.SubTotal.Should().Be(200m);
        result.Lines[firstVariantId].DiscountTotal.Should().Be(15m);
        result.Lines[secondVariantId].DiscountTotal.Should().Be(15m);
        result.Lines[firstVariantId].TaxTotal.Should().Be(17m);
        result.Lines[secondVariantId].TaxTotal.Should().Be(8.5m);
        result.TaxTotal.Should().Be(25.5m);
        result.GrandTotal.Should().Be(210.5m);
    }

    // Burada yuvarlama gereken indirimin satırlara eksiksiz dağıtılıp sipariş tutarının korunabildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Preserve_Total_Discount_When_Proportional_Values_Round()
    {
        var firstVariantId = Guid.NewGuid();
        var secondVariantId = Guid.NewGuid();
        var service = new OrderPricingService();

        var result = service.Calculate(
            [
                new OrderPricingLine(firstVariantId, 33.33m, 20m),
                new OrderPricingLine(secondVariantId, 66.67m, 20m)
            ],
            discountTotal: 10m,
            shippingTotal: 0m);

        result.Lines.Values.Sum(line => line.DiscountTotal).Should().Be(10m);
        result.TaxTotal.Should().Be(18m);
        result.GrandTotal.Should().Be(108m);
    }
}
