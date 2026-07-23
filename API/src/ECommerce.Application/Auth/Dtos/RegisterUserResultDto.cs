using ECommerce.Application.Users.Dtos;

namespace ECommerce.Application.Auth.Dtos;

public sealed record RegisterUserResultDto(
    UserDto User);
