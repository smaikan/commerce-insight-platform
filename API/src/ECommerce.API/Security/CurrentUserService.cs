using System.Security.Claims;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.API.Security;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return PublicIdCodec.TryDecodeUserId(value, out var userId) ? userId : null;
        }
    }
}
