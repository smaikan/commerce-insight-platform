using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public Guid TypeId { get; private set; }
    public ProductType Type { get; private set; } = null!;
    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public int ClickCount { get; private set; }
    public int TotalAddToCartCount { get; private set; }
    public int TotalPurchaseCount { get; private set; }
    public int FavoriteCount { get; private set; }
    public decimal AverageRating { get; private set; }
    public int RatingCount { get; private set; }
    public int ReviewCount { get; private set; }

    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public ICollection<ProductCollection> ProductCollections { get; private set; } = new List<ProductCollection>();
    public ICollection<ProductTag> ProductTags { get; private set; } = new List<ProductTag>();
    public ICollection<ProductDailyMetric> DailyMetrics { get; private set; } = new List<ProductDailyMetric>();
    public ICollection<ProductRating> Ratings { get; private set; } = new List<ProductRating>();
    public ICollection<ProductReview> Reviews { get; private set; } = new List<ProductReview>();
    public ICollection<FavoriteProduct> Favorites { get; private set; } = new List<FavoriteProduct>();
    public ICollection<ProductBundleItem> BundleItems { get; private set; } = new List<ProductBundleItem>();

    private Product()
    {
    }

    public Product(
        string title,
        string url,
        Guid typeId,
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
    }

    public void IncreaseClickCount()
    {
        ClickCount++;
        MarkAsUpdated();
    }

    public void IncreaseTotalAddToCartCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        TotalAddToCartCount += quantity;
        MarkAsUpdated();
    }

    public void IncreaseTotalPurchaseCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        TotalPurchaseCount += quantity;
        MarkAsUpdated();
    }

    public void IncreaseFavoriteCount()
    {
        FavoriteCount++;
        MarkAsUpdated();
    }

    public void DecreaseFavoriteCount()
    {
        if (FavoriteCount <= 0)
        {
            throw new DomainException("Favorite count cannot become negative.");
        }

        FavoriteCount--;
        MarkAsUpdated();
    }

    public void UpdateRatingSummary(decimal averageRating, int ratingCount)
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
        MarkAsUpdated();
    }

    public void IncreaseReviewCount()
    {
        ReviewCount++;
        MarkAsUpdated();
    }

    public void DecreaseReviewCount()
    {
        if (ReviewCount <= 0)
        {
            throw new DomainException("Review count cannot become negative.");
        }

        ReviewCount--;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void MarkAsFeatured()
    {
        IsFeatured = true;
        MarkAsUpdated();
    }

    public void UnmarkAsFeatured()
    {
        IsFeatured = false;
        MarkAsUpdated();
    }

    public void ChangeStatus(ProductStatus status)
    {
        Status = status;
        MarkAsUpdated();
    }

    public void ChangeType(Guid typeId)
    {
        SetType(typeId);
        MarkAsUpdated();
    }

    public void ChangeBrand(Guid? brandId)
    {
        SetBrand(brandId);
        MarkAsUpdated();
    }

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
        MarkAsUpdated();
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Product title cannot be empty.");
        }

        Title = title.Trim();
    }

    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Product url cannot be empty.");
        }

        Url = url.Trim();
    }

    private void SetType(Guid typeId)
    {
        if (typeId == Guid.Empty)
        {
            throw new DomainException("Product type is required.");
        }

        TypeId = typeId;
    }

    private void SetBrand(Guid? brandId)
    {
        if (brandId == Guid.Empty)
        {
            throw new DomainException("Brand id cannot be empty.");
        }

        BrandId = brandId;
    }

    private void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        DisplayOrder = displayOrder;
    }
}
