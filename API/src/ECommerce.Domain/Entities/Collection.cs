using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Collection : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }

    public ICollection<ProductCollection> ProductCollections { get; private set; } = new List<ProductCollection>();

    private Collection()
    {
    }

    public Collection(
        string name,
        string url,
        string? description = null,
        bool isActive = true,
        bool isFeatured = false,
        int displayOrder = 0)
    {
        SetName(name);
        SetUrl(url);
        ApplyDisplayOrder(displayOrder);
        Description = description?.Trim();
        IsActive = isActive;
        IsFeatured = isFeatured;
    }

    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    public void ChangeUrl(string url)
    {
        SetUrl(url);
        MarkAsUpdated();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        ApplyDisplayOrder(displayOrder);
        MarkAsUpdated();
    }

    private void ApplyDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        DisplayOrder = displayOrder;
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

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Collection name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Collection url cannot be empty.");
        }

        Url = url.Trim();
    }
}
