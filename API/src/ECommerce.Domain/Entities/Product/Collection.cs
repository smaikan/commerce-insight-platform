using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Collection : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string Url { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }

    public ICollection<ProductCollection> ProductCollections { get; private set; } = new List<ProductCollection>();

    // Burada EF Core'un koleksiyonu veri tabanından oluşturmasına izin veriyorum.
    private Collection()
    {
    }

    // Burada koleksiyonun temel alanlarını ve isteğe bağlı görsel URL değerini oluşturuyorum.
    public Collection(
        string name,
        string url,
        string? description = null,
        bool isActive = true,
        bool isFeatured = false,
        int displayOrder = 0,
        string? imageUrl = null)
    {
        SetName(name);
        SetUrl(url);
        ApplyDisplayOrder(displayOrder);
        Description = description?.Trim();
        ApplyImageUrl(imageUrl);
        IsActive = isActive;
        IsFeatured = isFeatured;
    }

    // Burada koleksiyon adını doğrulayarak değiştiriyorum.
    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    // Burada koleksiyon URL değerini doğrulayarak değiştiriyorum.
    public void ChangeUrl(string url)
    {
        SetUrl(url);
        MarkAsUpdated();
    }

    // Burada koleksiyon açıklamasını temizleyerek değiştiriyorum.
    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    // Burada koleksiyonun görsel URL değerini güncelliyorum.
    public void SetImageUrl(string? imageUrl)
    {
        ApplyImageUrl(imageUrl);
        MarkAsUpdated();
    }

    // Burada koleksiyonun gösterim sırasını değiştiriyorum.
    public void SetDisplayOrder(int displayOrder)
    {
        ApplyDisplayOrder(displayOrder);
        MarkAsUpdated();
    }

    // Burada gösterim sırasının negatif olmamasını sağlıyorum.
    private void ApplyDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        DisplayOrder = displayOrder;
    }

    // Burada koleksiyonu kullanıma açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada koleksiyonu kullanıma kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada koleksiyonu öne çıkarıyorum.
    public void MarkAsFeatured()
    {
        IsFeatured = true;
        MarkAsUpdated();
    }

    // Burada koleksiyonu öne çıkarılmış durumdan çıkarıyorum.
    public void UnmarkAsFeatured()
    {
        IsFeatured = false;
        MarkAsUpdated();
    }

    // Burada koleksiyon adını zorunlu ve temizlenmiş biçimde saklıyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Collection name cannot be empty.");
        }

        Name = name.Trim();
    }

    // Burada koleksiyon URL değerini zorunlu ve temizlenmiş biçimde saklıyorum.
    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Collection url cannot be empty.");
        }

        Url = url.Trim();
    }

    // Burada boş görsel URL değerini null olarak, dolu değeri temizleyerek saklıyorum.
    private void ApplyImageUrl(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
