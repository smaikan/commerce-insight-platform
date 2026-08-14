using ECommerce.Application.GuestSessions.Dtos;
using MediatR;

namespace ECommerce.Application.GuestSessions.Commands.ClaimGuestSession;

// Burada login sonrasında claim edilecek ortak guest session değerini taşıyorum.
public sealed record ClaimGuestSessionCommand(string SessionId) : IRequest<GuestSessionClaimDto>;
