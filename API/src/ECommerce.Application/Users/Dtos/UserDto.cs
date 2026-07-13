using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role,
    UserStatus Status,
    bool EmailConfirmed,
    int AccessFailedCount,
    DateTime? LockoutEndAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class UserDtoMapping
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role,
            user.Status,
            user.EmailConfirmed,
            user.AccessFailedCount,
            user.LockoutEndAt,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
