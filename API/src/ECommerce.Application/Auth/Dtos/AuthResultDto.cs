using ECommerce.Application.Users.Dtos;

namespace ECommerce.Application.Auth.Dtos;

public sealed record AuthResultDto(UserDto User, AuthTokensDto Tokens);
