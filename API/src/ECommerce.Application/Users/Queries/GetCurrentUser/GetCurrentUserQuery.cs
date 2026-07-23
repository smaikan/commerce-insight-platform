using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;
