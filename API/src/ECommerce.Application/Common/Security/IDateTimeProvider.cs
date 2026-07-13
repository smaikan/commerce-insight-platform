namespace ECommerce.Application.Common.Security;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
