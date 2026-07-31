using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class ReturnItem : BaseEntity
{
    public const int MaximumProductTitleLength = OrderItem.MaximumProductTitleLength;
    public const int MaximumVariantSkuLength = OrderItem.MaximumVariantSkuLength;

    public Guid ReturnRequestId { get; private set; }
    public ReturnRequest ReturnRequest { get; private set; } = null!;
    public Guid OrderItemId { get; private set; }
    public OrderItem OrderItem { get; private set; } = null!;
    public long ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductTitleSnapshot { get; private set; } = null!;
    public string VariantSkuSnapshot { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal RefundTotal { get; private set; }
    public Guid? ReplacementProductVariantId { get; private set; }

    // Burada EF Core'un iade kalemini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ReturnItem()
    {
    }

    // Burada iade kaleminin sipariş snapshot ve değişim varyantı kurallarını koruyarak kaydını hazırlıyorum.
    internal ReturnItem(
        ReturnRequest returnRequest,
        Guid orderItemId,
        long productId,
        Guid productVariantId,
        string productTitleSnapshot,
        string variantSkuSnapshot,
        decimal unitPrice,
        int quantity,
        Guid? replacementProductVariantId,
        decimal? refundTotal)
    {
        if (returnRequest is null || returnRequest.Id == Guid.Empty)
        {
            throw new DomainException("Return request is required.");
        }

        if (orderItemId == Guid.Empty || productId <= 0 || productVariantId == Guid.Empty)
        {
            throw new DomainException("Order item, product and product variant ids are required.");
        }

        if (replacementProductVariantId == Guid.Empty)
        {
            throw new DomainException("Replacement product variant id cannot be empty.");
        }

        if (returnRequest.Type == ReturnType.Refund && replacementProductVariantId.HasValue)
        {
            throw new DomainException("A refund return item cannot have a replacement product variant.");
        }

        if (returnRequest.Type == ReturnType.Exchange)
        {
            if (!replacementProductVariantId.HasValue)
            {
                throw new DomainException("An exchange return item requires a replacement product variant.");
            }

            if (replacementProductVariantId.Value == productVariantId)
            {
                throw new DomainException("An exchange replacement product variant must differ from the returned variant.");
            }
        }

        ReturnRequestId = returnRequest.Id;
        ReturnRequest = returnRequest;
        OrderItemId = orderItemId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductTitleSnapshot = NormalizeSnapshot(productTitleSnapshot, MaximumProductTitleLength, "Product title snapshot");
        VariantSkuSnapshot = NormalizeSnapshot(variantSkuSnapshot, MaximumVariantSkuLength, "Variant SKU snapshot");
        UnitPrice = ValidateUnitPrice(unitPrice);
        Quantity = ValidateQuantity(quantity);
        LineTotal = CalculateLineTotal(UnitPrice, Quantity);
        RefundTotal = ValidateRefundTotal(returnRequest.Type, refundTotal, LineTotal);
        ReplacementProductVariantId = replacementProductVariantId;
    }

    // Burada iade kaleminin ürün bedeli ile adedinden desteklenen para sınırında toplamı hesaplıyorum.
    private static decimal CalculateLineTotal(decimal unitPrice, int quantity)
    {
        decimal lineTotal;
        try
        {
            lineTotal = checked(unitPrice * quantity);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Return item total exceeds the supported monetary limit.", exception);
        }

        if (lineTotal > OrderItem.MaximumSupportedAmount)
        {
            throw new DomainException("Return item total exceeds the supported monetary limit.");
        }

        return lineTotal;
    }

    // Burada iade snapshot metnini boşluk ve veritabanı uzunluk kurallarına göre normalize ediyorum.
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

    // Burada iade birim fiyatının pozitif, iki ondalıklı ve desteklenen aralıkta olduğunu doğruluyorum.
    private static decimal ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0m)
        {
            throw new DomainException("Return item unit price must be greater than zero.");
        }

        if (decimal.Round(unitPrice, OrderItem.SupportedPriceScale) != unitPrice)
        {
            throw new DomainException($"Return item unit price cannot have more than {OrderItem.SupportedPriceScale} decimal places.");
        }

        if (unitPrice > OrderItem.MaximumSupportedAmount)
        {
            throw new DomainException("Return item unit price exceeds the supported monetary limit.");
        }

        return unitPrice;
    }

    // Burada iade kalemi adedinin pozitif kaldığını doğruluyorum.
    private static int ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Return item quantity must be greater than zero.");
        }

        return quantity;
    }

    // Burada iade tutarını yalnız refund akışında, iki ondalıklı ve güvenilir sipariş snapshot aralığında saklıyorum.
    private static decimal ValidateRefundTotal(
        ReturnType returnType,
        decimal? refundTotal,
        decimal lineTotal)
    {
        if (returnType == ReturnType.Exchange)
        {
            if (refundTotal is not null and not 0m)
            {
                throw new DomainException("An exchange return item cannot have a refund total.");
            }

            return 0m;
        }

        var resolvedRefundTotal = refundTotal ?? lineTotal;
        if (resolvedRefundTotal < 0m || resolvedRefundTotal > OrderItem.MaximumSupportedAmount)
        {
            throw new DomainException("Return item refund total is outside the supported range.");
        }

        if (decimal.Round(resolvedRefundTotal, OrderItem.SupportedPriceScale) != resolvedRefundTotal)
        {
            throw new DomainException($"Return item refund total cannot have more than {OrderItem.SupportedPriceScale} decimal places.");
        }

        return resolvedRefundTotal;
    }
}
