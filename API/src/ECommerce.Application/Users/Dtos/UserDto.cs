using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Users.Dtos;

public sealed record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role,
    UserStatus Status,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class UserDtoMapping
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            PublicIdCodec.EncodeUserId(user.Id),
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role,
            user.Status,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
