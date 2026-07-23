using ECommerce.Application.Common.Exceptions;

namespace ECommerce.Application.Common.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
}

public static class CurrentUserServiceExtensions
{
    public static long GetRequiredUserId(this ICurrentUserService currentUserService)
    {
        return currentUserService.UserId is > 0 and { } userId
            ? userId
            : throw new UnauthorizedException("Authenticated user is required.");
    }
}
