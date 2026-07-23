namespace ECommerce.Domain.Common;

public abstract class AuditableEntity<TId> : BaseEntity<TId>
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

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity()
    {
        Id = Guid.NewGuid();
    }
}
