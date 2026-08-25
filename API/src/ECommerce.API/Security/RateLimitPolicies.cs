namespace ECommerce.API.Security;

// Burada auth rate-limit adlarını controller ve başlangıç yapılandırması arasında yazım hatasına kapalı tutuyorum.
public static class RateLimitPolicies
{
    public const string AuthLogin = "auth-login";
    public const string AuthRegister = "auth-register";
    public const string AuthRefresh = "auth-refresh";
}
