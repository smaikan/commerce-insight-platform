using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductType : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private ProductType()
    {
    }

    public ProductType(string name, string? description = null, bool isActive = true)
    {
        SetName(name);
        Description = description?.Trim();
        IsActive = isActive;
    }

    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
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

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product type name cannot be empty.");
        }

        Name = name.Trim();
    }
}
