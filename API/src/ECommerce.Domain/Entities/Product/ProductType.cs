using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductType : AuditableEntity
{
    public const int MaximumImageUrlLength = 500;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    // Burada EF Core'un ürün türünü veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductType()
    {
    }

    // Burada ürün türünü isteğe bağlı vitrin görseliyle oluşturuyorum.
    public ProductType(
        string name,
        string? description = null,
        bool isActive = true,
        string? imageUrl = null)
    {
        SetName(name);
        Description = description?.Trim();
        ApplyImageUrl(imageUrl);
        IsActive = isActive;
    }

    // Burada ürün türünün adını doğrulayıp değiştiriyorum.
    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    // Burada ürün türünün açıklamasını temizleyerek değiştiriyorum.
    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    // Burada ürün türünün özel vitrin görselini güncelliyorum.
    public void SetImageUrl(string? imageUrl)
    {
        ApplyImageUrl(imageUrl);
        MarkAsUpdated();
    }

    // Burada ürün türünü public katalogda kullanılabilir hale getiriyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada ürün türünü public katalog kullanımından çıkarıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada ürün türü adının boş kalmasını engelleyip temizlenmiş değeri saklıyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product type name cannot be empty.");
        }

        Name = name.Trim();
    }

    // Burada boş görsel URL değerini null, dolu değeri temizlenmiş biçimde saklıyorum.
    private void ApplyImageUrl(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
