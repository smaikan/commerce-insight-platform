using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Services;

public sealed class UserTokenCleanupService : IUserTokenCleanupService
{
    private readonly AppDbContext _context;

    public UserTokenCleanupService(AppDbContext context)
    {
        _context = context;
    }

    // Burada saklama süresi geçen refresh ve parola sıfırlama tokenlarını topluca temizliyorum.
    public async Task<int> CleanupAsync(DateTime retentionCutoff, CancellationToken cancellationToken = default)
    {
        var deletedRefreshTokens = await _context.UserRefreshTokens
            .Where(token =>
                token.ExpiresAt < retentionCutoff ||
                (token.RevokedAt != null && token.RevokedAt < retentionCutoff))
            .ExecuteDeleteAsync(cancellationToken);
        var deletedSecurityTokens = await _context.UserSecurityTokens
            .Where(token =>
                token.ExpiresAt < retentionCutoff ||
                (token.UsedAt != null && token.UsedAt < retentionCutoff) ||
                (token.InvalidatedAt != null && token.InvalidatedAt < retentionCutoff))
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRefreshTokens + deletedSecurityTokens;
    }
}
