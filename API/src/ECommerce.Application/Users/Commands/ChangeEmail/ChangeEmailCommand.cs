using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Commands.ChangeEmail;

public sealed record ChangeEmailCommand(string CurrentPassword, string NewEmail) : IRequest<UserDto>;
