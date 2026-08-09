using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Users.Dtos;

public sealed record AdminUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role,
    UserStatus Status,
    DateTime? LastLoginAt,
    DateTime? PasswordChangedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int OrderCount = 0);

public static class AdminUserDtoMapping
{
    // Burada yönetim kullanıcı cevabını isteğe bağlı sipariş sayısıyla oluşturuyorum.
    public static AdminUserDto ToAdminDto(this User user, int orderCount = 0)
    {
        return new AdminUserDto(
            PublicIdCodec.EncodeUserId(user.Id),
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role,
            user.Status,
            user.LastLoginAt,
            user.PasswordChangedAt,
            user.CreatedAt,
            user.UpdatedAt,
            orderCount);
    }
}
