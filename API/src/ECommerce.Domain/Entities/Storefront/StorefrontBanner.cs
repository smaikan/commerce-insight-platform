using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class StorefrontBanner : AuditableEntity
{
    public StorefrontBannerSlot Slot { get; private set; }
    public string ImageUrl { get; private set; } = null!;

    // Burada EF Core'un banner kaydını veri tabanından oluşturmasına izin veriyorum.
    private StorefrontBanner()
    {
    }

    // Burada sabit banner alanını geçerli bir görsel URL değeriyle oluşturuyorum.
    public StorefrontBanner(StorefrontBannerSlot slot, string imageUrl)
    {
        if (!Enum.IsDefined(slot))
        {
            throw new DomainException("Storefront banner slot is invalid.");
        }

        Slot = slot;
        ApplyImageUrl(imageUrl);
    }

    // Burada banner görsel URL değerini değiştiriyorum.
    public void UpdateImageUrl(string imageUrl)
    {
        ApplyImageUrl(imageUrl);
        MarkAsUpdated();
    }

    // Burada banner görsel URL değerinin boş olmamasını sağlıyorum.
    private void ApplyImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new DomainException("Storefront banner image url cannot be empty.");
        }

        ImageUrl = imageUrl.Trim();
    }
}
