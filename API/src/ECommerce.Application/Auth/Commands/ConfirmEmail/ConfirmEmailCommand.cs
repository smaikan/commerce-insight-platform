using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : IRequest<UserDto>;
