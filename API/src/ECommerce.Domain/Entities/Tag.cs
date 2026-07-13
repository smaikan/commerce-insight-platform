using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Tag : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public ICollection<ProductTag> ProductTags { get; private set; } = new List<ProductTag>();

    private Tag()
    {
    }

    public Tag(string name, string url, bool isActive = true)
    {
        SetName(name);
        SetUrl(url);
        IsActive = isActive;
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
            throw new DomainException("Tag name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Tag url cannot be empty.");
        }

        Url = url.Trim();
    }
}
