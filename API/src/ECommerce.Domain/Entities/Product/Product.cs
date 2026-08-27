using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Product : AuditableEntity<long>
{
    public const int MaximumMainSkuLength = 100;
    public const int ClickScoreWeight = 1;
    public const int FavoriteScoreWeight = 4;
    public const int AddToCartScoreWeight = 8;
    public const int PurchaseScoreWeight = 20;

    public string Title { get; private set; } = null!;
    public string MainSku { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public Guid? TypeId { get; private set; }
    public ProductType? Type { get; private set; }
    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public Guid? TaxRateId { get; private set; }
    public TaxRate? TaxRate { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public long ClickCount { get; private set; }
    public long TotalAddToCartCount { get; private set; }
    public long TotalPurchaseCount { get; private set; }
    public long NetSalesQuantity { get; private set; }
    public long FavoriteCount { get; private set; }
    public long PopularityScore { get; private set; }
    public decimal AverageRating { get; private set; }
    public long RatingCount { get; private set; }
    public long ReviewCount { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public bool IsDeleted => DeletedAtUtc.HasValue;

    // Burada ürünün varyant içerip içermediğini gerçek varyant koleksiyonundan türetiyorum.
    public bool HasVariants { get; private set; }

    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public ICollection<ProductUrlRedirect> UrlRedirects { get; private set; } = new List<ProductUrlRedirect>();
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
        string mainSku,
        Guid? typeId = null,
        Guid? brandId = null,
        string? description = null,
        ProductStatus status = ProductStatus.Draft,
        bool isActive = true,
        bool isFeatured = false,
        int displayOrder = 0,
        string? seoTitle = null,
        string? seoDescription = null,
        Guid? taxRateId = null,
        bool hasVariants = false)
    {
        SetTitle(title);
        SetUrl(url);
        SetMainSku(mainSku);
        SetType(typeId);
        SetBrand(brandId);
        SetTaxRate(taxRateId);
        SetDisplayOrder(displayOrder);
        HasVariants = hasVariants;

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

    // Burada kesinleşmiş ücretli satış adedini popülerlik metriğinden bağımsız artırıyorum.
    public void IncreaseNetSalesQuantity(long quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Net sales quantity must be greater than zero.");
        }

        try
        {
            NetSalesQuantity = checked(NetSalesQuantity + quantity);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Net sales quantity exceeds the supported range.", exception);
        }

        MarkAsChanged();
    }

    // Burada kesin finansal ters işlem gören satış adedini negatif sonuca izin vermeden azaltıyorum.
    public void DecreaseNetSalesQuantity(long quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Net sales quantity must be greater than zero.");
        }

        if (quantity > NetSalesQuantity)
        {
            throw new DomainException("Net sales quantity cannot become negative.");
        }

        NetSalesQuantity -= quantity;
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

    // Burada harici katalogdan gelen ürün performans özetini tüm türetilmiş alanlarıyla
    // birlikte güvenli biçimde senkronize ediyorum. Popülerlik puanı dışarıdan kabul edilmez.
    public void ReplacePerformanceMetrics(
        long clickCount,
        long totalAddToCartCount,
        long totalPurchaseCount,
        long favoriteCount,
        decimal averageRating,
        long ratingCount,
        long reviewCount)
    {
        if (clickCount < 0 || totalAddToCartCount < 0 || totalPurchaseCount < 0 ||
            favoriteCount < 0 || ratingCount < 0 || reviewCount < 0)
        {
            throw new DomainException("Product performance metrics cannot be negative.");
        }

        if (averageRating < 0m || averageRating > 5m || decimal.Round(averageRating, 2) != averageRating)
        {
            throw new DomainException("Average rating must be between 0 and 5 with at most two decimal places.");
        }

        if (ratingCount == 0 && averageRating != 0m)
        {
            throw new DomainException("Average rating must be zero when there are no ratings.");
        }

        try
        {
            ClickCount = clickCount;
            TotalAddToCartCount = totalAddToCartCount;
            TotalPurchaseCount = totalPurchaseCount;
            FavoriteCount = favoriteCount;
            AverageRating = averageRating;
            RatingCount = ratingCount;
            ReviewCount = reviewCount;
            PopularityScore = checked(
                (clickCount * ClickScoreWeight) +
                (favoriteCount * FavoriteScoreWeight) +
                (totalAddToCartCount * AddToCartScoreWeight) +
                (totalPurchaseCount * PurchaseScoreWeight));
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Product popularity score exceeds the supported range.", exception);
        }

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
        EnsureNotDeleted();
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
        EnsureNotDeleted();
        Status = status;
        MarkAsChanged();
    }

    // Burada ürünü geçmiş kayıtlarını koruyarak katalogdan kaldırıyorum.
    public bool SoftDelete(DateTime deletedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            return false;
        }

        if (deletedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Product deletion time must be UTC.");
        }

        DeletedAtUtc = deletedAtUtc;
        Status = ProductStatus.Archived;
        IsActive = false;
        IsFeatured = false;
        MarkAsChanged();
        return true;
    }

    // Burada ürünün bağlı olduğu türü değiştiriyorum.
    public void ChangeType(Guid? typeId)
    {
        SetType(typeId);
        MarkAsChanged();
    }

    public void SetHasVariants(bool hasVariants)
    {
        if (!hasVariants && Variants.Count > 1)
        {
            throw new DomainException("A product with multiple variants must have HasVariants enabled.");
        }

        HasVariants = hasVariants;
        MarkAsChanged();
    }

    // Burada ürünün bağlı olduğu markayı değiştiriyorum.
    public void ChangeBrand(Guid? brandId)
    {
        SetBrand(brandId);
        MarkAsChanged();
    }

    // Burada ürünün seçili vergi oranını değiştiriyorum.
    public void ChangeTaxRate(Guid? taxRateId)
    {
        SetTaxRate(taxRateId);
        MarkAsChanged();
    }

    // Burada ürünün temel metin ve gösterim bilgilerini birlikte güncelliyorum.
    public void UpdateBasics(
        string title,
        string url,
        string? description,
        int displayOrder,
        string? seoTitle,
        string? seoDescription,
        string? mainSku = null)
    {
        SetTitle(title);
        foreach (var image in Images.Where(image => image.IsAltTextAutoGenerated))
        {
            image.ApplyAutomaticAltText(Title);
        }
        SetUrl(url);
        if (mainSku is not null)
        {
            SetMainSku(mainSku);
        }

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

    // Burada ana SKU bilgisini zorunlu, kısa ve karşılaştırılabilir büyük harfli biçimde saklıyorum.
    private void SetMainSku(string mainSku)
    {
        if (string.IsNullOrWhiteSpace(mainSku))
        {
            throw new DomainException("Product main SKU cannot be empty.");
        }

        var normalizedMainSku = mainSku.Trim().ToUpperInvariant();
        if (normalizedMainSku.Length > MaximumMainSkuLength)
        {
            throw new DomainException($"Product main SKU cannot exceed {MaximumMainSkuLength} characters.");
        }

        MainSku = normalizedMainSku;
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

    // Burada isteğe bağlı vergi oranı kimliğinin boş GUID olmadığını kontrol ediyorum.
    private void SetTaxRate(Guid? taxRateId)
    {
        if (taxRateId == Guid.Empty)
        {
            throw new DomainException("Tax rate id cannot be empty.");
        }

        TaxRateId = taxRateId;
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

    // Burada silinmiş ürünün tekrar aktif katalog durumuna alınmasını engelliyorum.
    private void EnsureNotDeleted()
    {
        if (DeletedAtUtc.HasValue)
        {
            throw new DomainException("A deleted product cannot be changed.");
        }
    }

    // Burada ürün değişikliğini concurrency ve audit alanlarına yansıtıyorum.
    private void MarkAsChanged()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }
}
