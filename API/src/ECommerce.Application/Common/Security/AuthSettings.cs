namespace ECommerce.Application.Common.Security;

public sealed class AuthSettings
{
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;
    public int PasswordResetTokenMinutes { get; init; } = 30;
    public int PasswordResetRequestCooldownSeconds { get; init; } = 120;
}
