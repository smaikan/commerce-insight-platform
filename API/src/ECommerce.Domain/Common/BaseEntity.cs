namespace ECommerce.Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
}

public abstract class BaseEntity : BaseEntity<Guid>
{

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
