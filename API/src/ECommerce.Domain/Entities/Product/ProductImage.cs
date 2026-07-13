using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductImage : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string ImageUrl { get; private set; } = null!;
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsMain { get; private set; }

    private ProductImage()
    {
    }

    public ProductImage(Guid productId, string imageUrl, int displayOrder = 0, bool isMain = false, string? altText = null)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        SetImageUrl(imageUrl);
        SetDisplayOrder(displayOrder);
        AltText = altText?.Trim();
        IsMain = isMain;
    }

    public void Update(string imageUrl, string? altText, int displayOrder, bool isMain)
    {
        SetImageUrl(imageUrl);
        SetDisplayOrder(displayOrder);
        AltText = altText?.Trim();
        IsMain = isMain;
        MarkAsUpdated();
    }

    private void SetImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new DomainException("Image url cannot be empty.");
        }

        ImageUrl = imageUrl.Trim();
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
