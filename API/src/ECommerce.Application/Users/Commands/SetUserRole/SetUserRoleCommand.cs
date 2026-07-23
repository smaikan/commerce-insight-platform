using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Users.Commands.SetUserRole;

public sealed record SetUserRoleCommand(long UserId, UserRole Role) : IRequest<AdminUserDto>;
