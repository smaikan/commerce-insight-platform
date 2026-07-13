using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni kullanıcıyı veritabanı takibine ekliyorum.
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    // Burada kullanıcıyı okuma amaçlı takip etmeden getiriyorum.
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    // Burada kullanıcıyı güncelleme için takipli şekilde getiriyorum.
    public Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Include(user => user.SecurityTokens)
            .Include(user => user.RefreshTokens)
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
            .Include(user => user.SecurityTokens)
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    // Burada refresh token hash bilgisinden oturum sahibi kullanıcıyı takipli şekilde getiriyorum.
    public Task<User?> GetByRefreshTokenHashForUpdateAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        var normalizedHash = refreshTokenHash.Trim();

        return _context.Users
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(
                user => user.RefreshTokens.Any(token => token.TokenHash == normalizedHash),
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
    public Task<bool> EmailExistsAsync(string email, Guid? excludedUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _context.Users.AnyAsync(
            user => user.Email == normalizedEmail && (!excludedUserId.HasValue || user.Id != excludedUserId.Value),
            cancellationToken);
    }
}
