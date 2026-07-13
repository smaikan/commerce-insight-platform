namespace ECommerce.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
