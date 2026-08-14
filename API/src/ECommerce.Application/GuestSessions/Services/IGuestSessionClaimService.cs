using ECommerce.Application.GuestSessions.Dtos;

namespace ECommerce.Application.GuestSessions.Services;

public interface IGuestSessionClaimService
{
    // Burada guest session verilerini kayıtlı kullanıcıya öncelik kurallarıyla devretme sözleşmesini tanımlıyorum.
    Task<GuestSessionClaimDto> ClaimAsync(
        long userId,
        string sessionId,
        CancellationToken cancellationToken = default);
}
