namespace ECommerce.Application.Common.Exceptions;

// Burada belirli HTTP durum ve ProblemDetails kodu gerektiren güvenli uygulama hatasını tanımlıyorum.
public class ApiContractException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string Title { get; }

    // Burada güvenli API hata sözleşmesinin durum, kod, başlık ve açıklamasını taşıyorum.
    public ApiContractException(int statusCode, string errorCode, string title, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Title = title;
    }
}

// Burada guest tarafından üyeye özel kupon kullanımını sabit 409 sözleşmesiyle reddediyorum.
public sealed class CouponMembersOnlyException : ApiContractException
{
    // Burada üyelik gerektiren kupon için istemcinin güvenle gösterebileceği hatayı oluşturuyorum.
    public CouponMembersOnlyException()
        : base(409, "coupon_members_only", "Coupon requires membership", "Bu kupon yalnızca üyeler içindir.")
    {
    }
}
