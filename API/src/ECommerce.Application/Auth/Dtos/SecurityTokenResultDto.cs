namespace ECommerce.Application.Auth.Dtos;

public sealed record SecurityTokenResultDto(
    Guid UserId,
    string Token,
    DateTime ExpiresAt);
