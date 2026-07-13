using ECommerce.Application.Common.Security;

namespace ECommerce.Infrastructure.Security;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
