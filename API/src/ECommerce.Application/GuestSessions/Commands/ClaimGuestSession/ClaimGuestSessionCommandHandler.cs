using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.GuestSessions.Dtos;
using ECommerce.Application.GuestSessions.Services;
using MediatR;

namespace ECommerce.Application.GuestSessions.Commands.ClaimGuestSession;

public sealed class ClaimGuestSessionCommandHandler
    : IRequestHandler<ClaimGuestSessionCommand, GuestSessionClaimDto>
{
    private readonly IGuestSessionClaimService _claimService;
    private readonly ICurrentUserService _currentUser;

    // Burada authenticated kullanıcı ile ortak guest claim servisini hazırlıyorum.
    public ClaimGuestSessionCommandHandler(
        IGuestSessionClaimService claimService,
        ICurrentUserService currentUser)
    {
        _claimService = claimService;
        _currentUser = currentUser;
    }

    // Burada doğrulanmış kullanıcı adına ortak guest session verilerini claim ediyorum.
    public Task<GuestSessionClaimDto> Handle(
        ClaimGuestSessionCommand request,
        CancellationToken cancellationToken)
    {
        return _claimService.ClaimAsync(
            _currentUser.GetRequiredUserId(),
            request.SessionId,
            cancellationToken);
    }
}
