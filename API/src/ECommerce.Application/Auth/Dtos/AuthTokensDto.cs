namespace ECommerce.Application.Auth.Dtos;

public sealed record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
