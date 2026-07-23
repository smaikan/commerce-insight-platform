using ECommerce.Domain.Entities;

namespace ECommerce.Application.Users.Dtos;

public sealed record UserSessionDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? CreatedByIp,
    string? DeviceName);

public static class UserSessionDtoMapping
{
    public static UserSessionDto ToSessionDto(this UserRefreshToken token)
    {
        return new UserSessionDto(token.Id, token.CreatedAt, token.ExpiresAt, token.CreatedByIp, token.DeviceName);
    }
}
