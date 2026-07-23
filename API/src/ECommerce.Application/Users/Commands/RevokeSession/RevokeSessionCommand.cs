using MediatR;

namespace ECommerce.Application.Users.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : IRequest;
