using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Security;

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user, Guid sessionId);
}
