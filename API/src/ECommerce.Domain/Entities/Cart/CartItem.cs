using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class CartItem : BaseEntity
{
    public const int SupportedPriceScale = 2;
    public const decimal MaximumSupportedAmount = 9999999999999999.99m;

    public Guid CartId { get; private set; }
    public Cart Cart { get; private set; } = null!;
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => CalculateValidatedTotal(Quantity, UnitPrice);
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un sepet satırını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private CartItem()
    {
    }

    // Burada yalnızca bağlı sepetin oluşturabileceği doğrulanmış bir sepet satırı hazırlıyorum.
    internal CartItem(
        Cart cart,
        long productId,
        Guid productVariantId,
        int quantity,
        decimal unitPrice)
    {
        if (cart is null)
        {
            throw new DomainException("Cart is required.");
        }

        ValidateProductIdentifiers(productId, productVariantId);
        _ = CalculateValidatedTotal(quantity, unitPrice);

        CartId = cart.Id;
        Cart = cart;
        ProductId = productId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada aynı varyant yeniden eklendiğinde oluşacak adet ve satır toplamını state'i değiştirmeden hesaplıyorum.
    internal (int Quantity, decimal TotalPrice) PreviewIncrease(
        long productId,
        int quantity,
        decimal unitPrice)
    {
        if (ProductId != productId)
        {
            throw new DomainException("Product variant cannot belong to a different product.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        int newQuantity;
        try
        {
            newQuantity = checked(Quantity + quantity);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Cart item quantity exceeds the supported limit.", exception);
        }

        var totalPrice = CalculateValidatedTotal(newQuantity, unitPrice);
        return (newQuantity, totalPrice);
    }

    // Burada önceden doğrulanan adet ve fiyatı aynı anda sepet satırına uyguluyorum.
    internal void ApplyValues(int quantity, decimal unitPrice)
    {
        _ = CalculateValidatedTotal(quantity, unitPrice);
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Burada sepet satırının adedini pozitif ve hesaplanabilir olacak şekilde güncelliyorum.
    internal void UpdateQuantity(int quantity)
    {
        _ = CalculateValidatedTotal(quantity, UnitPrice);
        Quantity = quantity;
    }

    // Burada sepet satırının birim fiyatını pozitif ve hesaplanabilir olacak şekilde güncelliyorum.
    internal void UpdateUnitPrice(decimal unitPrice)
    {
        _ = CalculateValidatedTotal(Quantity, unitPrice);
        UnitPrice = unitPrice;
    }

    // Burada adet ve birim fiyattan oluşacak satır toplamını para hassasiyetine göre doğruluyorum.
    internal static decimal CalculateValidatedTotal(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        ValidateUnitPrice(unitPrice);

        decimal totalPrice;
        try
        {
            totalPrice = checked(unitPrice * quantity);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Cart item total price exceeds the supported limit.", exception);
        }

        if (totalPrice > MaximumSupportedAmount)
        {
            throw new DomainException("Cart item total price exceeds the supported monetary limit.");
        }

        return totalPrice;
    }

    // Burada ürün ve varyant kimliklerinin geçerli olduğunu doğruluyorum.
    private static void ValidateProductIdentifiers(long productId, Guid productVariantId)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id must be greater than zero.");
        }

        if (productVariantId == Guid.Empty)
        {
            throw new DomainException("Product variant id is required.");
        }
    }

    // Burada birim fiyatın pozitif, iki ondalıklı ve veritabanındaki para alanına uygun olduğunu doğruluyorum.
    private static void ValidateUnitPrice(decimal unitPrice)
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
    }
}
