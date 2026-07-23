using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery : IRequest<IReadOnlyList<UserSessionDto>>;
