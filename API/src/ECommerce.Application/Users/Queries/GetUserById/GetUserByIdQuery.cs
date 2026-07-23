using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(long Id) : IRequest<UserDto>;
