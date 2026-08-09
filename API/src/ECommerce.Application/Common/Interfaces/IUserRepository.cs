using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Users.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(UserRefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminUserDto>> GetListAsync(UserListFilter filter, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenHashForUpdateAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task<UserSecurityToken?> GetSecurityTokenForUpdateAsync(
        UserSecurityTokenType type,
        string tokenHash,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRefreshToken>> GetActiveRefreshTokensForUpdateAsync(
        long userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRefreshToken>> GetActiveRefreshTokensAsync(
        long userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSecurityToken>> GetActiveSecurityTokensForUpdateAsync(
        long userId,
        UserSecurityTokenType type,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task<UserRefreshToken?> GetRefreshTokenForUpdateAsync(
        long userId,
        Guid refreshTokenId,
        CancellationToken cancellationToken = default);
    Task<bool> IsAccessTokenValidAsync(
        long userId,
        int securityVersion,
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, long? excludedUserId = null, CancellationToken cancellationToken = default);
    Task<bool> HasAnotherActiveAdminAsync(long excludedUserId, CancellationToken cancellationToken = default);
}
