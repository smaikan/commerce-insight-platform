using MediatR;

namespace ECommerce.Application.Users.Commands.LogoutAllSessions;

public sealed record LogoutAllSessionsCommand : IRequest;
