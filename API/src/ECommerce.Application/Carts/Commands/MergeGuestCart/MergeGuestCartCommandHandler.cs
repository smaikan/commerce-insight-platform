using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.GuestSessions.Services;
using MediatR;

namespace ECommerce.Application.Carts.Commands.MergeGuestCart;

public sealed class MergeGuestCartCommandHandler
    : IRequestHandler<MergeGuestCartCommand, CartDto>
{
    private readonly IGuestSessionClaimService _claimService;
    private readonly ICurrentUserService _currentUser;

    // Burada eski cart merge endpointini ortak guest session claim servisine bağlayan bağımlılıkları hazırlıyorum.
    public MergeGuestCartCommandHandler(
        IGuestSessionClaimService claimService,
        ICurrentUserService currentUser)
    {
        _claimService = claimService;
        _currentUser = currentUser;
    }

    // Burada geriye uyumlu cart cevabını ortak cart ve favorites claim sonucundan döndürüyorum.
    public async Task<CartDto> Handle(
        MergeGuestCartCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _claimService.ClaimAsync(
            _currentUser.GetRequiredUserId(),
            request.SessionId,
            cancellationToken);
        return result.Cart;
    }
}
