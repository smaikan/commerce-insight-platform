using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Brand : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Brand()
    {
    }

    public Brand(string name, string url, string? description = null, bool isActive = true)
    {
        SetName(name);
        SetUrl(url);
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

    public void ChangeUrl(string url)
    {
        SetUrl(url);
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
            throw new DomainException("Brand name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Brand url cannot be empty.");
        }

        Url = url.Trim();
    }
}
