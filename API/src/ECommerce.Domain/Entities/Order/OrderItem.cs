using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public const int MaximumProductTitleLength = 250;
    public const int MaximumVariantSkuLength = 100;
    public const int SupportedPriceScale = 2;
    public const decimal MaximumSupportedAmount = 9999999999999999.99m;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public long ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductTitleSnapshot { get; private set; } = null!;
    public string VariantSkuSnapshot { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }
    public decimal DiscountTotal { get; private set; }
    // Burada eski siparişlerde kaynak şema oran bilgisini saklamadığından nullable vergi oranı snapshot'ını taşıyorum.
    public decimal? TaxRatePercentage { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal RefundTotal => TotalPrice - DiscountTotal + TaxTotal;

    // Burada EF Core'un sipariş kalemini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private OrderItem()
    {
    }

    // Burada yalnız bağlı sipariş aggregate'ının oluşturabileceği immutable snapshot kalemini hazırlıyorum.
    internal OrderItem(
        Order order,
        long productId,
        Guid productVariantId,
        string productTitleSnapshot,
        string variantSkuSnapshot,
        decimal unitPrice,
        int quantity,
        decimal discountTotal,
        decimal taxRatePercentage,
        decimal taxTotal)
    {
        if (order is null || order.Id == Guid.Empty || productId <= 0 || productVariantId == Guid.Empty)
        {
            throw new DomainException("Order, product and variant ids are required.");
        }

        OrderId = order.Id;
        Order = order;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductTitleSnapshot = NormalizeSnapshot(
            productTitleSnapshot,
            MaximumProductTitleLength,
            "Product title snapshot");
        VariantSkuSnapshot = NormalizeSnapshot(
            variantSkuSnapshot,
            MaximumVariantSkuLength,
            "Variant SKU snapshot");
        UnitPrice = ValidateUnitPrice(unitPrice);
        Quantity = ValidateQuantity(quantity);
        TotalPrice = CalculateTotalPrice(UnitPrice, Quantity);
        DiscountTotal = ValidateDiscountTotal(discountTotal, TotalPrice);
        var validatedTaxRatePercentage = ValidateTaxRatePercentage(taxRatePercentage);
        TaxRatePercentage = validatedTaxRatePercentage;
        TaxTotal = ValidateTaxTotal(taxTotal, TotalPrice, DiscountTotal, validatedTaxRatePercentage);
    }

    // Burada sipariş kaleminin adet ve birim fiyatından taşmasız toplam fiyatı hesaplıyorum.
    private static decimal CalculateTotalPrice(decimal unitPrice, int quantity)
    {
        decimal totalPrice;

        try
        {
            totalPrice = checked(unitPrice * quantity);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item total price exceeds the supported limit.", exception);
        }

        if (totalPrice > MaximumSupportedAmount)
        {
            throw new DomainException("Order item total price exceeds the supported monetary limit.");
        }

        return totalPrice;
    }

    // Burada snapshot metnini boşluk ve veritabanı uzunluk kurallarına göre normalize ediyorum.
    private static string NormalizeSnapshot(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    // Burada birim fiyatın pozitif, iki ondalıklı ve desteklenen para aralığında olduğunu doğruluyorum.
    private static decimal ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
        {
            throw new DomainException("Unit price must be greater than zero.");
        }

        if (decimal.Round(unitPrice, SupportedPriceScale) != unitPrice)
        {
            throw new DomainException($"Unit price cannot have more than {SupportedPriceScale} decimal places.");
        }

        if (unitPrice > MaximumSupportedAmount)
        {
            throw new DomainException("Unit price exceeds the supported monetary limit.");
        }

        return unitPrice;
    }

    // Burada sipariş kalemi adedinin pozitif kaldığını doğruluyorum.
    private static int ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        return quantity;
    }

    // Burada satır indirimini negatif olmayacak, iki ondalıklı ve ürün toplamını aşmayacak biçimde doğruluyorum.
    private static decimal ValidateDiscountTotal(decimal discountTotal, decimal totalPrice)
    {
        if (discountTotal < 0m || discountTotal > totalPrice)
        {
            throw new DomainException("Order item discount total must be between zero and the item total.");
        }

        if (decimal.Round(discountTotal, SupportedPriceScale) != discountTotal)
        {
            throw new DomainException($"Order item discount total cannot have more than {SupportedPriceScale} decimal places.");
        }

        return discountTotal;
    }

    // Burada vergi oranı snapshot'ının ürün vergisi sınırları ve iki ondalık hassasiyette kaldığını doğruluyorum.
    private static decimal ValidateTaxRatePercentage(decimal taxRatePercentage)
    {
        if (taxRatePercentage < TaxRate.MinimumRate || taxRatePercentage > TaxRate.MaximumRate)
        {
            throw new DomainException("Order item tax rate percentage is invalid.");
        }

        if (decimal.Round(taxRatePercentage, SupportedPriceScale) != taxRatePercentage)
        {
            throw new DomainException($"Order item tax rate cannot have more than {SupportedPriceScale} decimal places.");
        }

        return taxRatePercentage;
    }

    // Burada indirim sonrası vergi snapshot'ının hesaplanan iki ondalıklı değerle eşleştiğini doğruluyorum.
    private static decimal ValidateTaxTotal(
        decimal taxTotal,
        decimal totalPrice,
        decimal discountTotal,
        decimal taxRatePercentage)
    {
        if (taxTotal < 0m || taxTotal > MaximumSupportedAmount)
        {
            throw new DomainException("Order item tax total is outside the supported range.");
        }

        if (decimal.Round(taxTotal, SupportedPriceScale) != taxTotal)
        {
            throw new DomainException($"Order item tax total cannot have more than {SupportedPriceScale} decimal places.");
        }

        var expectedTaxTotal = decimal.Round(
            (totalPrice - discountTotal) * taxRatePercentage / 100m,
            SupportedPriceScale,
            MidpointRounding.AwayFromZero);
        if (taxTotal != expectedTaxTotal)
        {
            throw new DomainException("Order item tax total is not consistent with its tax rate and discount.");
        }

        decimal refundTotal;
        try
        {
            refundTotal = checked(totalPrice - discountTotal + taxTotal);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item refundable total exceeds the supported limit.", exception);
        }

        if (refundTotal > MaximumSupportedAmount)
        {
            throw new DomainException("Order item refundable total exceeds the supported monetary limit.");
        }

        return taxTotal;
    }
}
