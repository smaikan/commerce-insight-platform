using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders.Services;

// Burada checkout sırasında güvenilir satır tutarlarından vergi, indirim ve kargo toplamını hesaplayan servisi tanımlıyorum.
public sealed class OrderPricingService
{
    // Burada kupon indirimini satırlara kararlı biçimde dağıtıp vergi ve genel toplamı oluşturuyorum.
    public OrderPricingResult Calculate(
        IReadOnlyCollection<OrderPricingLine> lines,
        decimal discountTotal,
        decimal shippingTotal)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new DomainException("At least one order pricing line is required.");
        }

        if (discountTotal < 0m || shippingTotal < 0m)
        {
            throw new DomainException("Discount and shipping totals cannot be negative.");
        }

        var orderedLines = lines
            .OrderBy(line => line.ProductVariantId)
            .ToList();
        var subTotal = CalculateSubTotal(orderedLines);
        if (discountTotal > subTotal)
        {
            throw new DomainException("Discount total cannot exceed subtotal.");
        }

        var pricingByVariantId = new Dictionary<Guid, OrderLinePricing>(orderedLines.Count);
        decimal remainingDiscount = discountTotal;
        decimal remainingSubTotal = subTotal;
        decimal taxTotal = 0m;

        foreach (var line in orderedLines)
        {
            var allocatedDiscount = AllocateDiscount(
                line,
                remainingDiscount,
                remainingSubTotal);
            var taxableAmount = line.LineTotal - allocatedDiscount;
            var lineTax = CalculateLineTax(taxableAmount, line.TaxRatePercentage);
            pricingByVariantId.Add(
                line.ProductVariantId,
                new OrderLinePricing(allocatedDiscount, line.TaxRatePercentage, lineTax));
            remainingDiscount -= allocatedDiscount;
            remainingSubTotal -= line.LineTotal;
            taxTotal = checked(taxTotal + lineTax);
        }

        if (remainingDiscount != 0m)
        {
            throw new DomainException("Discount allocation could not be completed.");
        }

        var grandTotal = checked(subTotal - discountTotal + shippingTotal + taxTotal);
        return new OrderPricingResult(
            subTotal,
            discountTotal,
            shippingTotal,
            taxTotal,
            grandTotal,
            pricingByVariantId);
    }

    // Burada kalemlerin güvenilir brüt olmayan ara toplamını taşma denetimiyle topluyorum.
    private static decimal CalculateSubTotal(IReadOnlyCollection<OrderPricingLine> lines)
    {
        decimal subTotal = 0m;
        foreach (var line in lines)
        {
            line.EnsureValid();
            subTotal = checked(subTotal + line.LineTotal);
        }

        return subTotal;
    }

    // Burada kalan indirimi son satırdaki küsuratı da kapsayacak şekilde oransal dağıtıyorum.
    private static decimal AllocateDiscount(
        OrderPricingLine line,
        decimal remainingDiscount,
        decimal remainingSubTotal)
    {
        if (remainingDiscount == 0m)
        {
            return 0m;
        }

        if (remainingSubTotal <= 0m)
        {
            throw new DomainException("Discount allocation subtotal must be greater than zero.");
        }

        if (line.LineTotal == remainingSubTotal)
        {
            return remainingDiscount;
        }

        var proportionalDiscount = decimal.Round(
            remainingDiscount * line.LineTotal / remainingSubTotal,
            OrderItem.SupportedPriceScale,
            MidpointRounding.AwayFromZero);
        return Math.Min(proportionalDiscount, line.LineTotal);
    }

    // Burada indirim sonrası satır tutarı üzerinden iki ondalıklı vergi tutarını hesaplıyorum.
    private static decimal CalculateLineTax(decimal taxableAmount, decimal taxRatePercentage)
    {
        return decimal.Round(
            taxableAmount * taxRatePercentage / 100m,
            OrderItem.SupportedPriceScale,
            MidpointRounding.AwayFromZero);
    }
}

// Burada checkout fiyat hesabına girecek güvenilir satır toplamı ve vergi yüzdesini taşıyorum.
public sealed record OrderPricingLine(
    Guid ProductVariantId,
    decimal LineTotal,
    decimal TaxRatePercentage)
{
    // Burada fiyat satırının tekil varyant, pozitif tutar ve geçerli vergi oranı kurallarını kontrol ediyorum.
    public void EnsureValid()
    {
        if (ProductVariantId == Guid.Empty || LineTotal <= 0m)
        {
            throw new DomainException("Order pricing line must contain a product variant and a positive total.");
        }

        if (TaxRatePercentage < TaxRate.MinimumRate || TaxRatePercentage > TaxRate.MaximumRate)
        {
            throw new DomainException("Order pricing line tax rate is invalid.");
        }
    }
}

// Burada tek sipariş kaleminin indirim, vergi oranı ve vergi snapshot sonucunu taşıyorum.
public sealed record OrderLinePricing(
    decimal DiscountTotal,
    decimal TaxRatePercentage,
    decimal TaxTotal);

// Burada siparişin vergi ve kargo dahil güvenilir fiyat hesaplama sonucunu taşıyorum.
public sealed record OrderPricingResult(
    decimal SubTotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyDictionary<Guid, OrderLinePricing> Lines);
