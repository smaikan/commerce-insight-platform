using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Exceptions;

namespace ECommerce.API.Security;

public sealed class GuestSessionCookieManager
{
    public const string CookieName = "ecommerce_guest_cart";
    public const string CsrfHeaderName = "X-Guest-CSRF";
    public const string CookiePath = "/api";
    public const int TokenByteLength = 32;
    private const string LegacyCartCookiePath = "/api/cart";
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);
    private readonly IConfiguration _configuration;

    // Burada ortak guest session cookie güvenliği için yapılandırmayı hazırlıyorum.
    public GuestSessionCookieManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Burada JWT'yi önceliklendirip anonim istekte ortak guest session değerini üretiyor veya yeniliyorum.
    public string? GetSessionIdForAccess(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        EnsureInvalidBearerDoesNotFallbackToGuest(httpContext);
        var sessionId = GetExistingSessionId(httpContext.Request) ?? CreateSessionId();
        WriteCookie(httpContext.Response, sessionId);
        return sessionId;
    }

    // Burada login claim işlemi için mevcut ve canonical guest session cookie değerini okuyorum.
    public string? GetExistingSessionId(HttpRequest request)
    {
        return request.Cookies.TryGetValue(CookieName, out var sessionId) &&
               IsCanonicalToken(sessionId)
            ? sessionId
            : null;
    }

    // Burada guest mutation isteğinde session, trusted Origin ve double-submit header eşleşmesini zorluyorum.
    public string RequireSessionForMutation(HttpContext httpContext)
    {
        EnsureInvalidBearerDoesNotFallbackToGuest(httpContext);
        var sessionId = GetExistingSessionId(httpContext.Request)
            ?? throw new ApiContractException(
                StatusCodes.Status401Unauthorized,
                "invalid_guest_access",
                "Guest session required",
                "Guest mutasyonu öncesinde geçerli bir guest session gereklidir.");

        EnsureTrustedOrigin(httpContext.Request);
        var csrfHeader = httpContext.Request.Headers[CsrfHeaderName].ToString();
        if (!FixedTimeEquals(sessionId, csrfHeader))
        {
            throw new ApiContractException(
                StatusCodes.Status403Forbidden,
                "invalid_guest_access",
                "CSRF validation failed",
                "Guest mutasyonu için CSRF doğrulaması başarısız oldu.");
        }

        WriteCookie(httpContext.Response, sessionId);
        return sessionId;
    }

    // Burada claim tamamlandığında ortak ve eski path kapsamındaki guest cookie'lerini siliyorum.
    public void DeleteSessionCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, CreateCookieOptions(CookiePath));
        response.Cookies.Delete(CookieName, CreateCookieOptions(LegacyCartCookiePath));
    }

    // Burada yalnız API'nin ürettiği 256 bit uppercase hexadecimal token biçimini kabul ediyorum.
    public static bool IsCanonicalToken(string? sessionId)
    {
        return sessionId?.Length == TokenByteLength * 2 &&
               sessionId.All(character =>
                   character is >= '0' and <= '9' or >= 'A' and <= 'F');
    }

    // Burada kriptografik olarak rastgele yeni guest session değeri üretiyorum.
    private static string CreateSessionId()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenByteLength));
    }

    // Burada ortak guest session cookie'sini tüm API guest özelliklerine gönderilecek kapsamda yazıyorum.
    private static void WriteCookie(HttpResponse response, string sessionId)
    {
        response.Cookies.Append(CookieName, sessionId, CreateCookieOptions(CookiePath));
    }

    // Burada guest cookie için güvenli ve otuz günlük seçenekleri oluşturuyorum.
    private static CookieOptions CreateCookieOptions(string path)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = path,
            MaxAge = CookieLifetime,
            Expires = DateTimeOffset.UtcNow.Add(CookieLifetime)
        };
    }

    // Burada cookie tabanlı guest mutation kaynağını yapılandırılmış storefront originleriyle sınırlıyorum.
    private void EnsureTrustedOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        var trustedOrigins = (_configuration["GuestProtection:TrustedOrigins"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (string.IsNullOrWhiteSpace(origin) ||
            !trustedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ApiContractException(
                StatusCodes.Status403Forbidden,
                "invalid_guest_access",
                "Origin validation failed",
                "Guest mutasyonu için güvenilir bir Origin gereklidir.");
        }
    }

    // Burada gönderilmiş fakat doğrulanamamış Bearer tokenın sessizce guest sahipliğine düşmesini engelliyorum.
    private static void EnsureInvalidBearerDoesNotFallbackToGuest(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.Authorization.Count > 0 &&
            httpContext.User.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("The access token is invalid or expired.");
        }
    }

    // Burada CSRF header ile cookie değerini zamanlama sızıntısı oluşturmadan karşılaştırıyorum.
    private static bool FixedTimeEquals(string expected, string actual)
    {
        if (!IsCanonicalToken(actual))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(actual));
    }
}
