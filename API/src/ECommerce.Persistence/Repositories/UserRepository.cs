using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Users.Dtos;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    // Burada kullanıcı sorguları için veritabanı bağlamını hazırlıyorum.
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni kullanıcıyı veritabanı takibine ekliyorum.
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    // Burada yeni oturum tokenının EF tarafından güncelleme değil ekleme olarak izlenmesini sağlıyorum.
    public async Task AddRefreshTokenAsync(
        UserRefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        await _context.UserRefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    // Burada kullanıcıyı okuma amaçlı takip etmeden getiriyorum.
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    // Burada kullanıcıyı güncelleme için takipli şekilde getiriyorum.
    public Task<User?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    // Burada kullanıcıyı email değerine göre takip etmeden getiriyorum.
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    // Burada kullanıcıyı email değerine göre güncelleme için takipli şekilde getiriyorum.
    public Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _context.Users
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    // Burada kullanıcıları yönetim ekranı için sayfalı ve takip edilmeden getiriyorum.
    public async Task<PagedResult<AdminUserDto>> GetListAsync(
        UserListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(user =>
                user.Email.Contains(search) ||
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(user => user.Role == filter.Role.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(user => user.Status == filter.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new UserListProjection(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.Status,
                user.LastLoginAt,
                user.PasswordChangedAt,
                user.CreatedAt,
                user.UpdatedAt,
                _context.Orders.Count(order => order.UserId == user.Id)))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>(
            users.Select(user => new AdminUserDto(
                PublicIdCodec.EncodeUserId(user.Id),
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.Status,
                user.LastLoginAt,
                user.PasswordChangedAt,
                user.CreatedAt,
                user.UpdatedAt,
                user.OrderCount)).ToList(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    // Burada refresh token hash bilgisinden oturum sahibi kullanıcıyı takipli şekilde getiriyorum.
    public Task<User?> GetByRefreshTokenHashForUpdateAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        var normalizedHash = refreshTokenHash.Trim();

        return _context.Users
            .Include(user => user.RefreshTokens.Where(token => token.TokenHash == normalizedHash))
            .FirstOrDefaultAsync(
                user => user.RefreshTokens.Any(token => token.TokenHash == normalizedHash),
                cancellationToken);
    }

    // Burada parola değişiminde iptal edilecek aktif refresh tokenları hedefli şekilde getiriyorum.
    public async Task<IReadOnlyList<UserRefreshToken>> GetActiveRefreshTokensForUpdateAsync(
        long userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null &&
                token.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);
    }

    // Burada kullanıcının aktif oturumlarını yalnızca okuma amacıyla getiriyorum.
    public async Task<IReadOnlyList<UserRefreshToken>> GetActiveRefreshTokensAsync(
        long userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRefreshTokens
            .AsNoTracking()
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null &&
                token.ExpiresAt > utcNow)
            .OrderByDescending(token => token.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // Burada aynı türdeki kullanılabilir güvenlik tokenlarını hedefli şekilde getiriyorum.
    public async Task<IReadOnlyList<UserSecurityToken>> GetActiveSecurityTokensForUpdateAsync(
        long userId,
        UserSecurityTokenType type,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserSecurityTokens
            .Where(token =>
                token.UserId == userId &&
                token.Type == type &&
                token.UsedAt == null &&
                token.InvalidatedAt == null &&
                token.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);
    }

    // Burada kullanıcıya ait belirli bir oturumu iptal etmek için takipli getiriyorum.
    public Task<UserRefreshToken?> GetRefreshTokenForUpdateAsync(
        long userId,
        Guid refreshTokenId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserRefreshTokens.FirstOrDefaultAsync(
            token => token.UserId == userId && token.Id == refreshTokenId,
            cancellationToken);
    }

    // Burada access token içindeki güvenlik sürümünün güncel kullanıcıyla eşleştiğini kontrol ediyorum.
    public Task<bool> IsAccessTokenValidAsync(
        long userId,
        int securityVersion,
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(
            user =>
                user.Id == userId &&
                user.Status == UserStatus.Active &&
                user.SecurityVersion == securityVersion &&
                user.RefreshTokens.Any(token =>
                    token.Id == sessionId &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > utcNow),
            cancellationToken);
    }

    // Burada email veya şifre sıfırlama tokenını takipli şekilde getiriyorum.
    public Task<UserSecurityToken?> GetSecurityTokenForUpdateAsync(
        UserSecurityTokenType type,
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var normalizedHash = tokenHash.Trim();

        return _context.UserSecurityTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(
                token => token.Type == type && token.TokenHash == normalizedHash,
                cancellationToken);
    }

    // Burada email adresinin başka bir kullanıcıda kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> EmailExistsAsync(string email, long? excludedUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _context.Users.AnyAsync(
            user => user.Email == normalizedEmail && (!excludedUserId.HasValue || user.Id != excludedUserId.Value),
            cancellationToken);
    }

    // Burada hedef kullanıcı dışında aktif bir admin hesabı bulunduğunu kontrol ediyorum.
    public Task<bool> HasAnotherActiveAdminAsync(long excludedUserId, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(
            user =>
                user.Id != excludedUserId &&
                user.Role == UserRole.Admin &&
                user.Status == UserStatus.Active,
            cancellationToken);
    }

    private sealed record UserListProjection(
        long Id,
        string Email,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        UserRole Role,
        UserStatus Status,
        DateTime? LastLoginAt,
        DateTime? PasswordChangedAt,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        int OrderCount);
}
