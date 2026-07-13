using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenHashForUpdateAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task<UserSecurityToken?> GetSecurityTokenForUpdateAsync(
        UserSecurityTokenType type,
        string tokenHash,
        CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludedUserId = null, CancellationToken cancellationToken = default);
}
