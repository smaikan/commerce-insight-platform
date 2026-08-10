using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Brand : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string Url { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    // Burada EF Core'un marka kaydını veri tabanından oluşturmasına izin veriyorum.
    private Brand()
    {
    }

    // Burada markanın temel alanlarını ve isteğe bağlı görsel URL değerini oluşturuyorum.
    public Brand(
        string name,
        string url,
        string? description = null,
        bool isActive = true,
        string? imageUrl = null)
    {
        SetName(name);
        SetUrl(url);
        Description = description?.Trim();
        ApplyImageUrl(imageUrl);
        IsActive = isActive;
    }

    // Burada marka adını doğrulayarak değiştiriyorum.
    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    // Burada marka açıklamasını temizleyerek değiştiriyorum.
    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    // Burada marka görsel URL değerini güncelliyorum.
    public void SetImageUrl(string? imageUrl)
    {
        ApplyImageUrl(imageUrl);
        MarkAsUpdated();
    }

    // Burada marka URL değerini doğrulayarak değiştiriyorum.
    public void ChangeUrl(string url)
    {
        SetUrl(url);
        MarkAsUpdated();
    }

    // Burada markayı kullanıma açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada markayı kullanıma kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada marka adını zorunlu ve temizlenmiş biçimde saklıyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Brand name cannot be empty.");
        }

        Name = name.Trim();
    }

    // Burada marka URL değerini zorunlu ve temizlenmiş biçimde saklıyorum.
    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Brand url cannot be empty.");
        }

        Url = url.Trim();
    }

    // Burada boş görsel URL değerini null olarak, dolu değeri temizleyerek saklıyorum.
    private void ApplyImageUrl(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
