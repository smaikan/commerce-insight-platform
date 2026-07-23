using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Product : AuditableEntity<long>
{
    public const int ClickScoreWeight = 1;
    public const int FavoriteScoreWeight = 4;
    public const int AddToCartScoreWeight = 8;
    public const int PurchaseScoreWeight = 20;

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public Guid? TypeId { get; private set; }
    public ProductType? Type { get; private set; }
    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public long ClickCount { get; private set; }
    public long TotalAddToCartCount { get; private set; }
    public long TotalPurchaseCount { get; private set; }
    public long FavoriteCount { get; private set; }
    public long PopularityScore { get; private set; }
    public decimal AverageRating { get; private set; }
    public long RatingCount { get; private set; }
    public long ReviewCount { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public ICollection<ProductCollection> ProductCollections { get; private set; } = new List<ProductCollection>();
    public ICollection<ProductTag> ProductTags { get; private set; } = new List<ProductTag>();
    public ICollection<ProductDailyMetric> DailyMetrics { get; private set; } = new List<ProductDailyMetric>();
    public ICollection<ProductRating> Ratings { get; private set; } = new List<ProductRating>();
    public ICollection<ProductReview> Reviews { get; private set; } = new List<ProductReview>();
    public ICollection<FavoriteProduct> Favorites { get; private set; } = new List<FavoriteProduct>();
    public ICollection<ProductBundleItem> BundleItems { get; private set; } = new List<ProductBundleItem>();

    // Burada EF Core'un ürünü veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private Product()
    {
    }

    // Burada yeni ürünü temel katalog bilgileri ve başlangıç değerleriyle oluşturuyorum.
    public Product(
        string title,
        string url,
        Guid? typeId = null,
        Guid? brandId = null,
        string? description = null,
        ProductStatus status = ProductStatus.Draft,
        bool isActive = true,
        bool isFeatured = false,
        int displayOrder = 0,
        string? seoTitle = null,
        string? seoDescription = null)
    {
        SetTitle(title);
        SetUrl(url);
        SetType(typeId);
        SetBrand(brandId);
        SetDisplayOrder(displayOrder);

        Description = description?.Trim();
        Status = status;
        IsActive = isActive;
        IsFeatured = isFeatured;
        SeoTitle = seoTitle?.Trim();
        SeoDescription = seoDescription?.Trim();
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada ürün tıklamasını ve karşılık gelen popülerlik puanını artırıyorum.
    public void IncreaseClickCount()
    {
        ClickCount++;
        PopularityScore += ClickScoreWeight;
        MarkAsChanged();
    }

    // Burada sepete eklenen adetleri ve her adet için kazanılan puanı artırıyorum.
    public void IncreaseTotalAddToCartCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        TotalAddToCartCount += quantity;
        PopularityScore += (long)quantity * AddToCartScoreWeight;
        MarkAsChanged();
    }

    // Burada satın alınan adetleri ve her adet için kazanılan puanı artırıyorum.
    public void IncreaseTotalPurchaseCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        TotalPurchaseCount += quantity;
        PopularityScore += (long)quantity * PurchaseScoreWeight;
        MarkAsChanged();
    }

    // Burada favori sayısını ve favori puanını artırıyorum.
    public void IncreaseFavoriteCount()
    {
        FavoriteCount++;
        PopularityScore += FavoriteScoreWeight;
        MarkAsChanged();
    }

    // Burada favoriden çıkarılan ürünün sayısını ve puanını güvenli biçimde azaltıyorum.
    public void DecreaseFavoriteCount()
    {
        if (FavoriteCount <= 0)
        {
            throw new DomainException("Favorite count cannot become negative.");
        }

        FavoriteCount--;
        PopularityScore -= FavoriteScoreWeight;
        MarkAsChanged();
    }

    // Burada ürünün ortalama puan ve değerlendirme özetini güncelliyorum.
    public void UpdateRatingSummary(decimal averageRating, long ratingCount)
    {
        if (averageRating < 0m || averageRating > 5m)
        {
            throw new DomainException("Average rating must be between 0 and 5.");
        }

        if (ratingCount < 0)
        {
            throw new DomainException("Rating count cannot be negative.");
        }

        AverageRating = averageRating;
        RatingCount = ratingCount;
        MarkAsChanged();
    }

    // Burada onaylı yorum sayısını artırıyorum.
    public void IncreaseReviewCount()
    {
        ReviewCount++;
        MarkAsChanged();
    }

    // Burada onaylı yorum sayısını negatif olmayacak şekilde azaltıyorum.
    public void DecreaseReviewCount()
    {
        if (ReviewCount <= 0)
        {
            throw new DomainException("Review count cannot become negative.");
        }

        ReviewCount--;
        MarkAsChanged();
    }

    // Burada ürünü satışta kullanılabilir duruma getiriyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsChanged();
    }

    // Burada ürünü satışa kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsChanged();
    }

    // Burada ürünü öne çıkan ürün olarak işaretliyorum.
    public void MarkAsFeatured()
    {
        IsFeatured = true;
        MarkAsChanged();
    }

    // Burada ürünün öne çıkarılmış işaretini kaldırıyorum.
    public void UnmarkAsFeatured()
    {
        IsFeatured = false;
        MarkAsChanged();
    }

    // Burada ürünün yayın durumunu değiştiriyorum.
    public void ChangeStatus(ProductStatus status)
    {
        Status = status;
        MarkAsChanged();
    }

    // Burada ürünün bağlı olduğu türü değiştiriyorum.
    public void ChangeType(Guid? typeId)
    {
        SetType(typeId);
        MarkAsChanged();
    }

    // Burada ürünün bağlı olduğu markayı değiştiriyorum.
    public void ChangeBrand(Guid? brandId)
    {
        SetBrand(brandId);
        MarkAsChanged();
    }

    // Burada ürünün temel metin ve gösterim bilgilerini birlikte güncelliyorum.
    public void UpdateBasics(
        string title,
        string url,
        string? description,
        int displayOrder,
        string? seoTitle,
        string? seoDescription)
    {
        SetTitle(title);
        SetUrl(url);
        SetDisplayOrder(displayOrder);
        Description = description?.Trim();
        SeoTitle = seoTitle?.Trim();
        SeoDescription = seoDescription?.Trim();
        MarkAsChanged();
    }

    // Burada ürün ilişkileri değiştiğinde concurrency değerini yeniliyorum.
    public void MarkRelationsChanged()
    {
        MarkAsChanged();
    }

    // Burada ürünün en az bir satılabilir varyanta sahip olduğunu doğruluyorum.
    public void EnsureHasAtLeastOneVariant()
    {
        if (Variants.Count == 0)
        {
            throw new DomainException("A product must have at least one variant.");
        }
    }

    // Burada ürün başlığını doğrulayıp temizlenmiş biçimde saklıyorum.
    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Product title cannot be empty.");
        }

        Title = title.Trim();
    }

    // Burada ürün URL bilgisini doğrulayıp temizlenmiş biçimde saklıyorum.
    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Product url cannot be empty.");
        }

        Url = url.Trim();
    }

    // Burada isteğe bağlı ürün türü kimliğinin boş GUID olmadığını kontrol ediyorum.
    private void SetType(Guid? typeId)
    {
        if (typeId == Guid.Empty)
        {
            throw new DomainException("Product type id cannot be empty.");
        }

        TypeId = typeId;
    }

    // Burada isteğe bağlı marka kimliğinin boş GUID olmadığını kontrol ediyorum.
    private void SetBrand(Guid? brandId)
    {
        if (brandId == Guid.Empty)
        {
            throw new DomainException("Brand id cannot be empty.");
        }

        BrandId = brandId;
    }

    // Burada manuel gösterim sırasının negatif olmadığını kontrol ediyorum.
    private void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        DisplayOrder = displayOrder;
    }

    // Burada ürün değişikliğini concurrency ve audit alanlarına yansıtıyorum.
    private void MarkAsChanged()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }
}
