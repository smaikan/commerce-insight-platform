using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductDailyMetric : BaseEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public long ClickCount { get; private set; }
    public long AddToCartCount { get; private set; }
    public long PurchaseCount { get; private set; }
    public long FavoriteCount { get; private set; }
    public long RatingCount { get; private set; }
    public long ReviewCount { get; private set; }

    // Burada EF Core'un günlük metriği veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductDailyMetric()
    {
    }

    // Burada ürünün belirli bir gününe ait metrik kaydını oluşturuyorum.
    public ProductDailyMetric(long productId, DateOnly date)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        Date = date;
    }

    // Burada günlük tıklama sayısını artırıyorum.
    public void IncreaseClickCount()
    {
        ClickCount++;
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

    // Burada günlük favori sayısını artırıyorum.
    public void IncreaseFavoriteCount()
    {
        FavoriteCount++;
    }

    // Burada günlük puanlama sayısını artırıyorum.
    public void IncreaseRatingCount()
    {
        RatingCount++;
    }

    // Burada günlük yorum sayısını artırıyorum.
    public void IncreaseReviewCount()
    {
        ReviewCount++;
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
