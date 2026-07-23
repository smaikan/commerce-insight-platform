using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Users.Commands.SetUserStatus;

public sealed record SetUserStatusCommand(long UserId, UserStatus Status) : IRequest<AdminUserDto>;
