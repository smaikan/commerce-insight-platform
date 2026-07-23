using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariantDailyMetric : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public long AddToCartCount { get; private set; }
    public long PurchaseCount { get; private set; }

    // Burada EF Core'un varyant metriğini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductVariantDailyMetric()
    {
    }

    // Burada varyantın belirli bir gününe ait metrik kaydını oluşturuyorum.
    public ProductVariantDailyMetric(Guid productVariantId, DateOnly date)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new DomainException("Product variant id is required.");
        }

        ProductVariantId = productVariantId;
        Date = date;
    }

    // Burada günlük sepete ekleme sayısını artırıyorum.
    public void IncreaseAddToCartCount(int quantity)
    {
        ValidateQuantity(quantity);
        AddToCartCount += quantity;
    }

    // Burada günlük satın alma sayısını artırıyorum.
    public void IncreasePurchaseCount(int quantity)
    {
        ValidateQuantity(quantity);
        PurchaseCount += quantity;
    }

    // Burada sayaçlara eklenecek miktarın pozitif olduğunu doğruluyorum.
    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }
}
