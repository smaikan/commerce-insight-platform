namespace ECommerce.Application.Common.Security;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
