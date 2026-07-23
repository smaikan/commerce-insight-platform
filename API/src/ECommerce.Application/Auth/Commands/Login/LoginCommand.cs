using ECommerce.Application.Auth.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? DeviceName = null) : IRequest<AuthResultDto>;
