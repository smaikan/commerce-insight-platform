using ECommerce.Domain.Common;

namespace ECommerce.UnitTests.Testing;

internal static class TestEntityIdentity
{
    public static T WithId<T>(this T entity, long id)
        where T : BaseEntity<long>
    {
        typeof(BaseEntity<long>)
            .GetProperty(nameof(BaseEntity<long>.Id))!
            .SetValue(entity, id);

        return entity;
    }
}
