namespace ECommerce.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
